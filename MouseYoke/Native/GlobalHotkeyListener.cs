using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace MouseYoke.Native;

public sealed class HotkeyCombo
{
    public bool Control { get; init; }
    public bool Shift { get; init; }
    public bool Alt { get; init; }
    public Key Key { get; init; } = Key.Y;

    public override string ToString()
    {
        var parts = new List<string>();
        if (Control) parts.Add("Ctrl");
        if (Shift) parts.Add("Shift");
        if (Alt) parts.Add("Alt");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}

/// <summary>
/// Listens for a global key combo via a low-level keyboard hook, so it fires even while
/// MSFS (or any other fullscreen/focused app) owns keyboard focus. This is the same
/// mechanism used by legitimate flight-sim utilities (e.g. FSUIPC) for global hotkeys -
/// it observes systemwide input, it does not read or write any other process's memory.
/// </summary>
public sealed class GlobalHotkeyListener : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle = IntPtr.Zero;
    private bool _controlDown, _shiftDown, _altDown;
    private bool _comboActive;

    public HotkeyCombo Combo { get; set; }

    public event Action? HotkeyPressed;

    public GlobalHotkeyListener(HotkeyCombo combo)
    {
        Combo = combo;
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero) return;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        if (_hookHandle == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to install the global keyboard hook (Win32 error {Marshal.GetLastWin32Error()}).");
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
            int vkCode = Marshal.ReadInt32(lParam);
            var key = KeyInterop.KeyFromVirtualKey(vkCode);
            bool isDown = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
            bool isUp = wParam == WM_KEYUP || wParam == WM_SYSKEYUP;

            if (key is Key.LeftCtrl or Key.RightCtrl)
            {
                if (isDown) _controlDown = true;
                else if (isUp) _controlDown = false;
            }
            else if (key is Key.LeftShift or Key.RightShift)
            {
                if (isDown) _shiftDown = true;
                else if (isUp) _shiftDown = false;
            }
            else if (key is Key.LeftAlt or Key.RightAlt)
            {
                if (isDown) _altDown = true;
                else if (isUp) _altDown = false;
            }

            if (key == Combo.Key)
            {
                if (isDown)
                {
                    bool modifiersMatch = _controlDown == Combo.Control
                        && _shiftDown == Combo.Shift
                        && _altDown == Combo.Alt;

                    if (modifiersMatch && !_comboActive)
                    {
                        _comboActive = true;
                        HotkeyPressed?.Invoke();
                    }
                }
                else if (isUp)
                {
                    _comboActive = false;
                }
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
