using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.FlightSimulator.SimConnect;

namespace MouseYoke.Simulation;

public enum ControlEvent
{
    Aileron,
    Elevator,
    Throttle,
}

/// <summary>
/// Wraps the managed SimConnect SDK: maintains a connection to MSFS, transmits raw axis
/// input events (the same events a physical joystick/throttle axis would send, which is
/// the most broadly compatible way to reach default and payware aircraft alike), and
/// quietly retries in the background if the sim isn't running yet or gets restarted.
/// </summary>
public sealed class SimConnectClient : IDisposable
{
    private const int WM_USER_SIMCONNECT = 0x0402;
    private const string AppName = "MouseYoke";

    private enum NotificationGroup { Input }
    private enum RequestId { ThrottlePosition }
    private enum DefinitionId { ThrottlePosition }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    private struct ThrottleData
    {
        public double LeverPositionPercent;
    }

    private readonly HwndSource _messageWindow;
    private readonly DispatcherTimer _reconnectTimer;
    private SimConnect? _simConnect;

    public bool IsConnected { get; private set; }
    public event Action? Connected;
    public event Action? Disconnected;

    /// <summary>Fires with the sim's actual current throttle lever position (0..16384) in response to RequestCurrentThrottle().</summary>
    public event Action<int>? ThrottlePositionReceived;

    public SimConnectClient()
    {
        var parameters = new HwndSourceParameters(AppName + "MessageWindow")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE: message-only window, never visible
        };
        _messageWindow = new HwndSource(parameters);
        _messageWindow.AddHook(WndProc);

        _reconnectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _reconnectTimer.Tick += (_, _) => TryConnect();
    }

    public void Start()
    {
        TryConnect();
        _reconnectTimer.Start();
    }

    private void TryConnect()
    {
        if (IsConnected) return;

        try
        {
            _simConnect = new SimConnect(AppName, _messageWindow.Handle, WM_USER_SIMCONNECT, null, 0);

            _simConnect.OnRecvOpen += (_, _) =>
            {
                IsConnected = true;
                Connected?.Invoke();
            };
            _simConnect.OnRecvQuit += (_, _) => HandleDisconnect();
            _simConnect.OnRecvException += (_, _) => { /* ignore malformed/unsupported requests, keep the connection alive */ };
            _simConnect.OnRecvSimobjectData += OnRecvSimobjectData;

            foreach (ControlEvent evt in Enum.GetValues<ControlEvent>())
            {
                _simConnect.MapClientEventToSimEvent(evt, EventName(evt));
                _simConnect.AddClientEventToNotificationGroup(NotificationGroup.Input, evt, false);
            }
            _simConnect.SetNotificationGroupPriority(NotificationGroup.Input, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);

            _simConnect.AddToDataDefinition(
                DefinitionId.ThrottlePosition, "GENERAL ENG THROTTLE LEVER POSITION:1", "Percent",
                SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
            _simConnect.RegisterDataDefineStruct<ThrottleData>(DefinitionId.ThrottlePosition);
        }
        catch (COMException)
        {
            // MSFS isn't running / SimConnect isn't reachable yet - retried on the next timer tick.
            _simConnect = null;
        }
    }

    private void OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        if ((RequestId)data.dwRequestID != RequestId.ThrottlePosition || data.dwData.Length == 0) return;

        var throttle = (ThrottleData)data.dwData[0];
        int scaled = (int)Math.Round(Math.Clamp(throttle.LeverPositionPercent, 0, 100) / 100.0 * AxisMapper.SimThrottleMax);
        ThrottlePositionReceived?.Invoke(scaled);
    }

    private void HandleDisconnect()
    {
        IsConnected = false;
        _simConnect?.Dispose();
        _simConnect = null;
        Disconnected?.Invoke();
    }

    /// <summary>Sends a raw axis value (-16384..16384, throttle 0..16384) for the given control.</summary>
    public void SendAxis(ControlEvent evt, int value)
    {
        if (!IsConnected || _simConnect is null) return;

        try
        {
            _simConnect.TransmitClientEvent(
                0, evt, unchecked((uint)value), NotificationGroup.Input, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }
        catch (COMException)
        {
            HandleDisconnect();
        }
    }

    /// <summary>Asks the sim for the current throttle lever position; the result arrives via ThrottlePositionReceived. Used to resync before the first scroll notch of a session, avoiding a jarring jump from a stale cached value.</summary>
    public void RequestCurrentThrottle()
    {
        if (!IsConnected || _simConnect is null) return;

        try
        {
            _simConnect.RequestDataOnSimObject(
                RequestId.ThrottlePosition, DefinitionId.ThrottlePosition, 0u,
                SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0u, 0u, 0u);
        }
        catch (COMException)
        {
            // Best-effort resync; a stale cached throttle value is a minor annoyance, not fatal.
        }
    }

    private static string EventName(ControlEvent evt) => evt switch
    {
        ControlEvent.Aileron => "AXIS_AILERONS_SET",
        ControlEvent.Elevator => "AXIS_ELEVATOR_SET",
        ControlEvent.Throttle => "AXIS_THROTTLE_SET",
        _ => throw new ArgumentOutOfRangeException(nameof(evt)),
    };

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_USER_SIMCONNECT && _simConnect is not null)
        {
            try
            {
                _simConnect.ReceiveMessage();
            }
            catch (COMException)
            {
                HandleDisconnect();
            }
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _reconnectTimer.Stop();
        _simConnect?.Dispose();
        _messageWindow.RemoveHook(WndProc);
        _messageWindow.Dispose();
    }
}
