using System.Windows.Input;
using MouseYoke.Native;

namespace MouseYoke.Config;

public sealed class AppSettings
{
    public bool HotkeyControl { get; set; } = true;
    public bool HotkeyShift { get; set; } = false;
    public bool HotkeyAlt { get; set; } = false;
    public Key HotkeyKey { get; set; } = Key.Y;

    /// <summary>Side length of the yoke square, in physical screen pixels.</summary>
    public int SquareSize { get; set; } = 260;

    /// <summary>Where the square's center sits on the primary monitor, as a 0..1 fraction of screen width/height.</summary>
    public double SquareCenterXRatio { get; set; } = 0.5;
    public double SquareCenterYRatio { get; set; } = 0.5;

    /// <summary>Fraction of half-square-size around the center that produces zero deflection.</summary>
    public double Deadzone { get; set; } = 0.05;

    /// <summary>Response curve exponent: 1.0 = linear, &gt;1.0 = softer near center / sharper near the edges.</summary>
    public double ResponseCurve { get; set; } = 1.0;

    public bool InvertAileron { get; set; } = false;
    public bool InvertElevator { get; set; } = false;

    /// <summary>Percent of full idle-to-max throttle travel applied per scroll-wheel notch.</summary>
    public int ThrottleStepPercent { get; set; } = 5;

    public HotkeyCombo ToHotkeyCombo() => new()
    {
        Control = HotkeyControl,
        Shift = HotkeyShift,
        Alt = HotkeyAlt,
        Key = HotkeyKey,
    };
}
