using System;

namespace MouseYoke.Simulation;

public readonly record struct AxisOutput(int Aileron, int Elevator);

/// <summary>
/// Converts a cursor position inside the yoke square into SimConnect's axis range.
/// The square is a fixed absolute zone: its center is neutral, its edges are full
/// deflection, and the mapping is continuous position -> deflection (no click/drag).
/// </summary>
public static class AxisMapper
{
    public const int SimAxisMin = -16384;
    public const int SimAxisMax = 16384;

    // AXIS_THROTTLE_SET uses the full signed range: -16384 = idle (0%), +16384 = full power (100%).
    // It is NOT 0..16384 - sending only that half of the range means the sim can only ever see
    // roughly 50%-100% throttle, which is why throttle used to feel like it "never reached full".
    public const int SimThrottleMin = -16384;
    public const int SimThrottleMax = 16384;
    private const int SimThrottleSpan = SimThrottleMax - SimThrottleMin;

    public static AxisOutput Map(
        int cursorX, int cursorY,
        int squareLeft, int squareTop, int squareSize,
        double deadzone, double responseCurve,
        bool invertAileron, bool invertElevator)
    {
        double halfSize = squareSize / 2.0;
        double centerX = squareLeft + halfSize;
        double centerY = squareTop + halfSize;

        double normalizedX = Clamp((cursorX - centerX) / halfSize, -1.0, 1.0);
        // Screen Y grows downward, but SimConnect's elevator convention (and every other
        // flight sim / game's mouse-look) treats "mouse up" as "pitch up" (positive elevator),
        // so this is inverted relative to raw screen coordinates on purpose.
        double normalizedY = Clamp((centerY - cursorY) / halfSize, -1.0, 1.0);

        double aileron = ApplyDeadzoneAndCurve(normalizedX, deadzone, responseCurve);
        double elevator = ApplyDeadzoneAndCurve(normalizedY, deadzone, responseCurve);

        if (invertAileron) aileron = -aileron;
        if (invertElevator) elevator = -elevator;

        return new AxisOutput(
            (int)Math.Round(aileron * SimAxisMax),
            (int)Math.Round(elevator * SimAxisMax));
    }

    /// <summary>Converts an actual throttle lever position (0-100%) into SimConnect's signed axis range.</summary>
    public static int PercentToThrottleAxis(double percent) =>
        (int)Math.Round(SimThrottleMin + Clamp(percent, 0, 100) / 100.0 * SimThrottleSpan);

    /// <summary>Steps the running throttle value by one notch's worth of percent of the full idle-to-max range, clamped to valid.</summary>
    public static int StepThrottle(int currentValue, int wheelDelta, int stepPercent)
    {
        int step = (int)Math.Round(SimThrottleSpan * (stepPercent / 100.0));
        int direction = Math.Sign(wheelDelta);
        return Clamp(currentValue + direction * step, SimThrottleMin, SimThrottleMax);
    }

    private static double ApplyDeadzoneAndCurve(double normalized, double deadzone, double curve)
    {
        double magnitude = Math.Abs(normalized);
        if (magnitude <= deadzone) return 0.0;

        double rescaled = (magnitude - deadzone) / (1.0 - deadzone);
        double shaped = Math.Pow(Clamp(rescaled, 0.0, 1.0), curve);
        return Math.Sign(normalized) * shaped;
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
}
