using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.FlightSimulator.SimConnect;

namespace MouseYoke.Simulation;

public enum ControlEvent
{
    Aileron,
    Elevator,
}

/// <summary>
/// Wraps the managed SimConnect SDK: maintains a connection to MSFS, transmits raw axis
/// input events (the same events a physical joystick axis would send, which is the most
/// broadly compatible way to reach default and payware aircraft alike), and quietly retries
/// in the background if the sim isn't running yet or gets restarted.
/// </summary>
public sealed class SimConnectClient : IDisposable
{
    private const int WM_USER_SIMCONNECT = 0x0402;
    private const string AppName = "MouseYoke";

    private enum NotificationGroup { Input }

    private readonly HwndSource _messageWindow;
    private readonly DispatcherTimer _reconnectTimer;
    private SimConnect? _simConnect;
    private bool _connecting;
    private bool _disposed;

    public bool IsConnected { get; private set; }
    public event Action? Connected;
    public event Action? Disconnected;

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
        if (IsConnected || _connecting) return;
        _connecting = true;

        // The SimConnect constructor blocks synchronously while it probes for a running sim,
        // so it's run off the UI thread - otherwise every retry stalls the WH_MOUSE_LL hook
        // (pumped on this same thread) for the duration of the probe, causing a mouse stutter.
        Task.Run(() =>
        {
            SimConnect? simConnect = null;
            try
            {
                simConnect = new SimConnect(AppName, _messageWindow.Handle, WM_USER_SIMCONNECT, null, 0);

                foreach (ControlEvent evt in Enum.GetValues<ControlEvent>())
                {
                    simConnect.MapClientEventToSimEvent(evt, EventName(evt));
                    simConnect.AddClientEventToNotificationGroup(NotificationGroup.Input, evt, false);
                }
                simConnect.SetNotificationGroupPriority(NotificationGroup.Input, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);
            }
            catch (COMException)
            {
                // MSFS isn't running / SimConnect isn't reachable yet - retried on the next timer tick.
                simConnect?.Dispose();
                simConnect = null;
            }

            if (!_disposed) _messageWindow.Dispatcher.Invoke(() => FinishConnect(simConnect));
            else simConnect?.Dispose();
        });
    }

    private void FinishConnect(SimConnect? simConnect)
    {
        _connecting = false;
        if (simConnect is null || _disposed)
        {
            simConnect?.Dispose();
            return;
        }

        _simConnect = simConnect;
        _simConnect.OnRecvOpen += (_, _) =>
        {
            IsConnected = true;
            Connected?.Invoke();
        };
        _simConnect.OnRecvQuit += (_, _) => HandleDisconnect();
        _simConnect.OnRecvException += (_, _) => { /* ignore malformed/unsupported requests, keep the connection alive */ };
    }

    private void HandleDisconnect()
    {
        IsConnected = false;
        _simConnect?.Dispose();
        _simConnect = null;
        Disconnected?.Invoke();
    }

    /// <summary>Sends a raw axis value (-16384..16384) for the given control.</summary>
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

    private static string EventName(ControlEvent evt) => evt switch
    {
        ControlEvent.Aileron => "AXIS_AILERONS_SET",
        ControlEvent.Elevator => "AXIS_ELEVATOR_SET",
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
        _disposed = true;
        _reconnectTimer.Stop();
        _simConnect?.Dispose();
        _messageWindow.RemoveHook(WndProc);
        _messageWindow.Dispose();
    }
}
