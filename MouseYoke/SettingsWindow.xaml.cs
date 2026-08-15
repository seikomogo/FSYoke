using System;
using System.Windows;
using System.Windows.Input;
using MouseYoke.Config;

namespace MouseYoke;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public event Action<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        KeyCombo.ItemsSource = new[] { Key.Y, Key.Z, Key.X, Key.C, Key.V, Key.B, Key.J, Key.K, Key.L };
        KeyCombo.SelectedItem = _settings.HotkeyKey;

        CtrlCheck.IsChecked = _settings.HotkeyControl;
        ShiftCheck.IsChecked = _settings.HotkeyShift;
        AltCheck.IsChecked = _settings.HotkeyAlt;

        SquareSizeSlider.Value = _settings.SquareSize;
        DeadzoneSlider.Value = _settings.Deadzone;
        CurveSlider.Value = _settings.ResponseCurve;

        InvertAileronCheck.IsChecked = _settings.InvertAileron;
        InvertElevatorCheck.IsChecked = _settings.InvertElevator;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.HotkeyControl = CtrlCheck.IsChecked == true;
        _settings.HotkeyShift = ShiftCheck.IsChecked == true;
        _settings.HotkeyAlt = AltCheck.IsChecked == true;
        _settings.HotkeyKey = (Key)(KeyCombo.SelectedItem ?? Key.Y);

        _settings.SquareSize = (int)SquareSizeSlider.Value;
        _settings.Deadzone = DeadzoneSlider.Value;
        _settings.ResponseCurve = CurveSlider.Value;

        _settings.InvertAileron = InvertAileronCheck.IsChecked == true;
        _settings.InvertElevator = InvertElevatorCheck.IsChecked == true;

        SettingsService.Save(_settings);
        SettingsSaved?.Invoke(_settings);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
