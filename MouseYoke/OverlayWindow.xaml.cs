using System.Windows;
using System.Windows.Interop;
using MouseYoke.Native;

namespace MouseYoke;

/// <summary>
/// The transparent yoke square. Created once at startup (hidden) and click-through/no-activate
/// styled immediately, so it never steals focus from MSFS once shown. Positioning still goes
/// through <see cref="WindowInterop"/> for exact physical-pixel alignment with the mouse hook's
/// coordinates, but showing/hiding goes through WPF's own Show()/Hide() - a layered
/// (AllowsTransparency) window only starts actually compositing/rendering its content once WPF's
/// own Show() pipeline runs, so skipping it (as an earlier version of this file did, to dodge a
/// one-frame flicker) left the window "visible" at the Win32 level but with nothing ever painted
/// into it.
/// </summary>
public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
        new WindowInteropHelper(this).EnsureHandle();
        WindowInterop.MakeClickThrough(this);
    }

    public void ShowAt(int left, int top, int size)
    {
        Show();
        WindowInterop.SetBoundsAndShow(this, left, top, size);
    }

    public void HideOverlay() => Hide();

    /// <summary>Moves the live indicator dot to reflect the cursor's current raw position within the square (-1..1 per axis, screen-space).</summary>
    public void UpdateIndicator(double normalizedX, double normalizedY, int squareSize)
    {
        double halfSize = squareSize / 2.0;
        IndicatorTransform.X = halfSize * normalizedX;
        IndicatorTransform.Y = halfSize * normalizedY;
    }
}
