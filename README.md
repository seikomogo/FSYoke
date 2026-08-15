# FSYoke (MouseYoke)

A Windows tray utility that reproduces FSX's "mouse as yoke" feature for **Microsoft Flight Simulator 2024**. Press a hotkey (default `Ctrl+Y`) to pop up a small transparent square on your screen; move the mouse inside it to fly ailerons/elevator, scroll the wheel to control throttle.

## How it works

- **Hotkey** (default `Ctrl+Y`, remappable in Settings) toggles a small transparent, click-through square on screen.
- While the square is visible, your cursor's position **inside that square** is a fixed absolute control zone: dead center = neutral controls, the edges = full aileron/elevator deflection. There's no click-and-drag — just move the mouse.
- **Scroll wheel** adjusts throttle in small steps while the square is active. It doesn't hijack scrolling anywhere else on your system.
- Under the hood it drives MSFS through **SimConnect's raw axis input events** (`AXIS_AILERONS_SET`, `AXIS_ELEVATOR_SET`, `AXIS_THROTTLE_SET`) — the same events a physical joystick/throttle axis would send. That's deliberate: these hit the sim's input layer before aircraft-specific systems, so they work with the default fleet and the overwhelming majority of payware addons without per-aircraft setup.
- The square is a true click-through overlay (`WS_EX_TRANSPARENT`/layered window) — it never intercepts clicks, and it never steals focus from MSFS. It's purely a visual reference for where "neutral" and "full deflection" are.
- Rudder is intentionally **not** included in this version — FSX's own mouse yoke didn't support it either.

Turning the square off does **not** snap the controls back to neutral — it leaves ailerons/elevator at their last commanded position, exactly like releasing a physical control axis would. Center your mouse in the square before deactivating if you want a neutral handoff.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft Flight Simulator 2024 (Steam or Microsoft Store/Xbox app — both work, see the Store-specific note below)
- The free **MSFS 2024 SDK**. Unlike most SDKs, it isn't a standalone web download — it ships from inside the sim itself:
  1. In MSFS, go to **Settings > General > Developer Tools** (or **Advanced Options**, wording varies by build) and switch **Developer Mode** on.
  2. Restart/return to the sim — a **DevMode** menu bar appears across the top.
  3. Open **Help > SDK Installer** — this downloads an `.msi`. Run it; it installs to `C:\MSFS 2024 SDK\` by default and sets an `MSFS_SDK` environment variable that this project's `.csproj` uses to locate `SimConnect.dll`.
- Visual Studio 2022 (recommended) or just the `dotnet` CLI.

## Build

```bash
dotnet restore
dotnet build MouseYoke.sln -c Release
```

If the build fails to find `Microsoft.FlightSimulator.SimConnect`, confirm the `MSFS_SDK` environment variable is set (reopen your terminal/IDE after installing the SDK) and points at a folder containing `SimConnect SDK\lib\managed\Microsoft.FlightSimulator.SimConnect.dll`. If your SDK version lays files out differently, edit the two `HintPath`/`Include` paths in `MouseYoke/MouseYoke.csproj` directly.

## Run

```bash
dotnet run --project MouseYoke -c Release
```

Or run the built `MouseYoke.exe` from `MouseYoke\bin\Release\net8.0-windows\`. The app has no main window — look for its icon in the system tray. Right-click it for **Settings** (hotkey, square size, deadzone, response curve, invert axes, throttle step) and **Exit**.

You can launch MouseYoke before or after MSFS — it retries the SimConnect connection every 5 seconds in the background and recovers automatically if the sim restarts.

## Usage

1. Launch MSFS 2024 and get into a flight.
2. Launch `MouseYoke.exe`.
3. Press `Ctrl+Y` (or your configured hotkey) — a small transparent square appears on your primary monitor.
4. Move the mouse inside the square to control ailerons/elevator; scroll to adjust throttle.
5. Press the hotkey again to hide the square and get your mouse back for clicking MSFS's own UI/cockpit.

## Compatibility notes

- **Payware aircraft**: `AXIS_*_SET` events are the most broadly compatible input path SimConnect offers, but a handful of complex addons with fully custom input handling may still respond slightly differently — the same caveat a physical hardware axis would face. This is a known limitation of the SimConnect ecosystem, not something fixable from a client app.
- **Elevation mismatch**: if MSFS is running as Administrator, SimConnect's local named-pipe connection can fail to reach it from a non-elevated client. If MouseYoke can't connect and MSFS is running elevated, try running MouseYoke as Administrator too.
- **Microsoft Store/Xbox app install**: MSFS 2024 runs its SimConnect servers (pipe, IPv4, IPv6) the same way regardless of Steam vs. Store packaging, so `MouseYoke` connecting locally should just work. If it doesn't connect at all on a Store install, the fallback is to force a TCP connection instead of the named pipe: create a `SimConnect.cfg` next to `MouseYoke.exe` with
  ```ini
  [SimConnect]
  Protocol=IPv4
  Address=127.0.0.1
  Port=500
  ```
  (every MSFS 2024 install exposes TCP on port 500 by default). The Store build's own data lives under `%LOCALAPPDATA%\Packages\Microsoft.Limitless_8wekyb3d8bbwe\LocalCache\` if you need to inspect its `SimConnect.xml` for reference.
- **Multiple monitors**: the square always centers on your *primary* display by default; adjust its position via the settings ratios if you'd rather it appear elsewhere.
- **Using a physical yoke/joystick at the same time**: MouseYoke transmits at the highest SimConnect notification priority (matching how a real input device would compete for the axis), so simultaneous physical and mouse input on the same axis will fight each other, same as plugging in two physical controllers mapped to the same axis.

## Project layout

```
MouseYoke/
  App.xaml(.cs)                 App startup/shutdown, wires everything together
  OverlayWindow.xaml(.cs)       The transparent click-through square
  SettingsWindow.xaml(.cs)      Settings UI
  TrayIconManager.cs            System tray icon + menu
  Native/
    GlobalHotkeyListener.cs     Low-level keyboard hook for the global hotkey
    MouseTracker.cs             Low-level mouse hook for cursor position + wheel
    WindowInterop.cs            Click-through styling, physical-pixel window positioning
  Simulation/
    SimConnectClient.cs         SimConnect connection, event transmission, reconnect loop
    AxisMapper.cs                Pure cursor-position -> axis-value mapping logic
  Config/
    AppSettings.cs               Settings POCO
    SettingsService.cs           JSON persistence to %AppData%\MouseYoke\settings.json
```

## A note on testing

This machine has MSFS 2024 installed (Microsoft Store/Xbox app build, under `E:\Xbox\Microsoft Flight Simulator 2024`) but is missing the .NET 8 SDK and the MSFS SDK, so this has **not** been compiled or run yet. The SimConnect managed API calls follow the documented signatures and standard community sample patterns, but Microsoft has occasionally made small signature changes between SDK point releases — if `dotnet build` reports a mismatch in `Simulation/SimConnectClient.cs`, cross-check the exact method signature against your installed SDK's IntelliSense/XML docs. Please build it, fly with it, and treat the first session as a shakedown: confirm the square appears where expected, that aileron/elevator/throttle respond correctly (check MSFS's own input indicators), and that toggling off returns full mouse control.
