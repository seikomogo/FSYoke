using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MouseYoke.Native;

/// <summary>
/// Win32 interop for the overlay window: click-through input, no taskbar/focus presence,
/// and physical-pixel positioning that matches the coordinates reported by the low-level
/// mouse hook. WPF's own Left/Top/Width/Height are DPI-scaled logical units and would
/// drift out of sync with the hook's physical-pixel coordinates on scaled displays, so all
/// geometry here is deliberately done in raw screen pixels via SetWindowPos/GetSystemMetrics
/// instead.
/// </summary>
internal static class WindowInterop
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_HIDEWINDOW = 0x0080;

    private static readonly IntPtr HwndTopmost = new(-1);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    /// <summary>Makes the window pass all mouse/keyboard input through to whatever is beneath it, keeps it off the taskbar/alt-tab, and stops it from stealing focus (and thus MSFS's keyboard/mouse capture) when shown.</summary>
    public static void MakeClickThrough(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    public static void SetBoundsAndShow(Window window, int left, int top, int size)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        SetWindowPos(hwnd, HwndTopmost, left, top, size, size, SWP_SHOWWINDOW | SWP_NOACTIVATE);
    }

    public static void HideWindow(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_HIDEWINDOW | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>Primary monitor size in physical pixels. The yoke square is always positioned relative to the primary display.</summary>
    public static (int Width, int Height) GetPrimaryScreenSizePhysicalPixels()
        => (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));

    /// <summary>Warps the OS cursor to an absolute physical-pixel screen position.</summary>
    public static void WarpCursor(int x, int y) => SetCursorPos(x, y);
}
