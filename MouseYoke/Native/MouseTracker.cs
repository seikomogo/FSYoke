using System;
using System.Runtime.InteropServices;

namespace MouseYoke.Native;

/// <summary>
/// Reports absolute cursor position and scroll-wheel deltas (plus live Shift state) via a
/// low-level mouse hook, so both keep working regardless of which window currently has focus
/// (MSFS in particular). Mouse movement is always passed through untouched.
///
/// Wheel events are handed to <see cref="WheelScrolled"/>, which returns whether to swallow
/// that notch system-wide. Note this swallowing only affects the classic Win32 message queue -
/// modern DirectX games (MSFS included) typically read the wheel via Raw Input instead, which
/// bypasses low-level hooks entirely and cannot be blocked this way. That's why App.xaml.cs
/// requires a Shift modifier for throttle by default rather than relying on suppression: it
/// sidesteps the conflict with MSFS's own scroll-to-zoom by using an input combination MSFS
/// isn't already listening for, instead of trying (and mostly failing) to block the shared one.
/// </summary>
public sealed class MouseTracker : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_MOUSEWHEEL = 0x020A;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_SHIFT = 0x10;

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hookHandle = IntPtr.Zero;

    /// <summary>Fires on every global mouse move with the cursor's screen-space physical-pixel position.</summary>
    public event Action<int, int>? MouseMoved;

    /// <summary>
    /// Fires on every global wheel notch with (delta, isShiftHeld); positive delta = scroll
    /// up/away, negative = down/toward, magnitude 120 per notch. Return true to attempt to
    /// swallow that notch from reaching any other app - see the suppression caveat above.
    /// </summary>
    public Func<int, bool, bool>? WheelScrolled { get; set; }

    public MouseTracker()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero) return;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        if (_hookHandle == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to install the global mouse hook (Win32 error {Marshal.GetLastWin32Error()}).");
    }

    public void Stop()
    {
        if (_hookHandle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            if (wParam == WM_MOUSEMOVE)
            {
                MouseMoved?.Invoke(data.pt.X, data.pt.Y);
            }
            else if (wParam == WM_MOUSEWHEEL)
            {
                short delta = (short)((data.mouseData >> 16) & 0xFFFF);
                bool shiftHeld = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
                bool suppress = WheelScrolled?.Invoke(delta, shiftHeld) ?? false;

                if (suppress)
                {
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
