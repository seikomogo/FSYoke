# Development notes

Maintainer/build notes for MouseYoke. None of this is needed to just run the app — see
[README.md](README.md) for that.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft Flight Simulator 2024 (Steam or Microsoft Store/Xbox app)
- The free **MSFS 2024 SDK**. Unlike most SDKs, it isn't a standalone web download — it ships from inside the sim itself:
  1. In MSFS, go to **Settings > General > Developer Tools** (or **Advanced Options**, wording varies by build) and switch **Developer Mode** on.
  2. Restart/return to the sim — a **DevMode** menu bar appears across the top.
  3. Open **Help > SDK Installer** — this downloads an `.msi`. Run it; it installs to `C:\MSFS 2024 SDK\` by default and sets an `MSFS_SDK` environment variable that this project's `.csproj` uses to locate `SimConnect.dll`. This is a **build-time only** requirement — never needed by someone just running the finished app.
- Visual Studio 2022 (recommended) or just the `dotnet` CLI.

## Dev build/run

Fast inner loop, produces a normal multi-file output under `MouseYoke\bin\...`:

```bash
dotnet restore
dotnet build MouseYoke.sln -c Release
dotnet run --project MouseYoke -c Release
```

If the build fails to find `Microsoft.FlightSimulator.SimConnect`, confirm the `MSFS_SDK` environment variable is set (reopen your terminal/IDE after installing the SDK) and points at a folder containing `SimConnect SDK\lib\managed\Microsoft.FlightSimulator.SimConnect.dll`. If your SDK version lays files out differently, edit the two `HintPath`/`Include` paths in `MouseYoke/MouseYoke.csproj` directly.

## Producing the distributable

```powershell
.\publish.ps1
```

This runs `dotnet publish` as a self-contained, single-file, win-x64 build and drops the result in `dist\`. It's not a *literal* single file: `Microsoft.FlightSimulator.SimConnect.dll` is a mixed-mode (C++/CLI) assembly, and .NET's single-file publish feature cannot embed mixed-mode assemblies at all — it crashes on startup with a `BadImageFormatException` if forced. The `.csproj` explicitly excludes it (and the native `SimConnect.dll` it wraps) from the bundle via a `ResolvedFileToPublish` target, so they ship as two small loose files next to `MouseYoke.exe` instead. Still just 3 files total, versus the 10+ files (plus a separate .NET runtime install) a normal framework-dependent build would need.

Zip up `dist\`'s contents for a Release asset (`MouseYoke-vX.Y.Z-win-x64.zip`).

## Project layout

```
MouseYoke/
  Assets/
    app.ico                     Joystick tray/app icon (multi-res: 16/32/48/256px)
  App.xaml(.cs)                 App startup/shutdown, wires everything together
  OverlayWindow.xaml(.cs)       The transparent click-through square
  SettingsWindow.xaml(.cs)      Settings UI
  TrayIconManager.cs            System tray icon + menu
  Native/
    GlobalHotkeyListener.cs     Low-level keyboard hook for the global hotkey
    MouseTracker.cs             Low-level mouse hook for cursor position
    WindowInterop.cs            Click-through styling, physical-pixel window positioning
  Simulation/
    SimConnectClient.cs         SimConnect connection, event transmission, reconnect loop
    AxisMapper.cs                Pure cursor-position -> axis-value mapping logic
  Config/
    AppSettings.cs               Settings POCO
    SettingsService.cs           JSON persistence to %AppData%\MouseYoke\settings.json
publish.ps1                      Builds the distributable, see "Producing the distributable" above
NOTICE.md                        Third-party attribution for the redistributed SimConnect DLLs
```

## Design notes

- Drives MSFS through SimConnect's raw axis input events (`AXIS_AILERONS_SET`, `AXIS_ELEVATOR_SET`) — the same events a physical joystick axis would send, which hit the sim's input layer before aircraft-specific systems, so they work with the default fleet and the overwhelming majority of payware addons without per-aircraft setup.
- The overlay is a true click-through window (`WS_EX_TRANSPARENT`/layered) — it never intercepts clicks and never steals focus from MSFS.
- Global hotkey and mouse tracking use low-level Win32 hooks (`WH_KEYBOARD_LL`/`WH_MOUSE_LL`) since MSFS runs fullscreen/exclusive-focus in most setups.
- Throttle-via-scroll was implemented, tested live against MSFS 2024, found to permanently conflict with MSFS's own scroll-to-zoom (modern DirectX games read the wheel via Raw Input, which bypasses low-level hooks entirely), and removed rather than shipped half-working.
- Aileron/elevator direction and the overlay/indicator/auto-center behavior have all been verified end-to-end, including against the actual published self-contained `dist\MouseYoke.exe`, not just the dev build.
