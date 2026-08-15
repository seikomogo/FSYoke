using System.Windows;
using System.Windows.Interop;
using MouseYoke.Native;

namespace MouseYoke;

/// <summary>
/// The transparent yoke square. Created once at startup (hidden) and repositioned/shown or
/// hidden natively via <see cref="WindowInterop"/> on each hotkey toggle - it never goes
/// through WPF's own Show()/Hide(), which avoids a visible flicker back to its default
/// position and keeps it from ever stealing focus from MSFS.
/// </summary>
public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
        new WindowInteropHelper(this).EnsureHandle();
        WindowInterop.MakeClickThrough(this);
    }

    public void ShowAt(int left, int top, int size) => WindowInterop.SetBoundsAndShow(this, left, top, size);

    public void HideOverlay() => WindowInterop.HideWindow(this);
}
