using System.Windows;
using MouseYoke.Config;
using MouseYoke.Native;
using MouseYoke.Simulation;

namespace MouseYoke;

public partial class App : Application
{
    private AppSettings _settings = null!;
    private OverlayWindow _overlay = null!;
    private SimConnectClient _simConnect = null!;
    private MouseTracker _mouseTracker = null!;
    private GlobalHotkeyListener _hotkeyListener = null!;
    private TrayIconManager _tray = null!;
    private SettingsWindow? _settingsWindow;

    private bool _isActive;
    private int _squareLeft, _squareTop;
    private int _throttleValue;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = SettingsService.Load();

        _overlay = new OverlayWindow();

        _simConnect = new SimConnectClient();
        _simConnect.ThrottlePositionReceived += value => _throttleValue = value;
        _simConnect.Start();

        _mouseTracker = new MouseTracker();
        _mouseTracker.MouseMoved += OnMouseMoved;
        _mouseTracker.WheelScrolled += OnWheelScrolled;
        _mouseTracker.Start();

        _hotkeyListener = new GlobalHotkeyListener(_settings.ToHotkeyCombo());
        _hotkeyListener.HotkeyPressed += ToggleActive;
        _hotkeyListener.Start();

        _tray = new TrayIconManager();
        _tray.SettingsRequested += ShowSettings;
        _tray.ExitRequested += Shutdown;
    }

    private void ToggleActive()
    {
        _isActive = !_isActive;
        _tray.SetActive(_isActive);

        if (_isActive)
        {
            _simConnect.RequestCurrentThrottle();
            (_squareLeft, _squareTop) = ComputeSquarePosition();
            _overlay.ShowAt(_squareLeft, _squareTop, _settings.SquareSize);
        }
        else
        {
            _overlay.HideOverlay();
        }
    }

    private (int left, int top) ComputeSquarePosition()
    {
        var (screenWidth, screenHeight) = WindowInterop.GetPrimaryScreenSizePhysicalPixels();
        int left = (int)(screenWidth * _settings.SquareCenterXRatio - _settings.SquareSize / 2.0);
        int top = (int)(screenHeight * _settings.SquareCenterYRatio - _settings.SquareSize / 2.0);
        return (left, top);
    }

    private void OnMouseMoved(int x, int y)
    {
        if (!_isActive) return;

        var output = AxisMapper.Map(
            x, y, _squareLeft, _squareTop, _settings.SquareSize,
            _settings.Deadzone, _settings.ResponseCurve,
            _settings.InvertAileron, _settings.InvertElevator);

        _simConnect.SendAxis(ControlEvent.Aileron, output.Aileron);
        _simConnect.SendAxis(ControlEvent.Elevator, output.Elevator);
    }

    private void OnWheelScrolled(int delta)
    {
        if (!_isActive) return;

        _throttleValue = AxisMapper.StepThrottle(_throttleValue, delta, _settings.ThrottleStepPercent);
        _simConnect.SendAxis(ControlEvent.Throttle, _throttleValue);
    }

    private void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings);
        _settingsWindow.SettingsSaved += updated =>
        {
            _settings = updated;
            _hotkeyListener.Combo = _settings.ToHotkeyCombo();
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyListener.Dispose();
        _mouseTracker.Dispose();
        _simConnect.Dispose();
        _tray.Dispose();
        _overlay.Close();
        base.OnExit(e);
    }
}
