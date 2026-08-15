# FSYoke (MouseYoke)

A Windows tray utility that reproduces FSX's "mouse as yoke" feature for **Microsoft Flight Simulator 2024**. Press a hotkey (default `Ctrl+Y`) to pop up a small transparent square on your screen; move the mouse inside it to fly ailerons/elevator.

## Download & run

No .NET install, no MSFS SDK, no build step - just the app.

1. Download `MouseYoke-vX.Y.Z-win-x64.zip` from the [Releases page](https://github.com/seikomogo/FSYoke/releases/latest) and extract it (or build it yourself with `publish.ps1` - see below). Keep `MouseYoke.exe`, `Microsoft.FlightSimulator.SimConnect.dll`, and `SimConnect.dll` together in the same folder.
2. Double-click `MouseYoke.exe`. Look for the joystick icon in your system tray (it may be tucked under the little "^" overflow arrow the first time).
3. Launch MSFS 2024, get into a flight, press `Ctrl+Y`.

That's it - jump to [Usage](#usage) below for the controls. `MouseYoke.exe` bundles the full .NET runtime, so it runs standalone; the two SimConnect DLLs have to stay next to it (they can't be embedded in the exe - see [Building from source](#building-from-source) for why).

## How it works

- **Hotkey** (default `Ctrl+Y`, remappable in Settings) toggles a small transparent, click-through square on screen (160px by default, resizable in Settings). Activating it warps your cursor to dead center of the square, so control always starts from neutral instead of jerking to wherever the mouse happened to be.
- While the square is visible, your cursor's position **inside that square** is a fixed absolute control zone: dead center = neutral controls, the edges = full aileron/elevator deflection. There's no click-and-drag — just move the mouse. A small dim dot marks the true center/neutral point; a brighter green dot tracks your cursor's live position within the square in real time.
- Under the hood it drives MSFS through **SimConnect's raw axis input events** (`AXIS_AILERONS_SET`, `AXIS_ELEVATOR_SET`) — the same events a physical joystick axis would send. That's deliberate: these hit the sim's input layer before aircraft-specific systems, so they work with the default fleet and the overwhelming majority of payware addons without per-aircraft setup.
- The square is a true click-through overlay (`WS_EX_TRANSPARENT`/layered window) — it never intercepts clicks, and it never steals focus from MSFS.
- Rudder and throttle are intentionally **not** included. FSX's own mouse yoke didn't do rudder either, and a scroll-wheel throttle turned out to be a dead end: MSFS's default scroll-to-zoom binding fires on *any* wheel movement, including modifier-key combinations like Shift+Scroll, and modern DirectX games (MSFS included) typically read the wheel via Raw Input — a delivery path that bypasses the kind of low-level input hook a desktop app can use to suppress it. There's no reliable way to give a scroll notch to this tool without MSFS also seeing it, so throttle-via-scroll was dropped rather than shipped half-working. (If you want scroll-wheel zoom disabled anyway, MSFS lets you clear or rebind it under **Options > Controls > Mouse Control > Zoom Cockpit View**.)

Turning the square off does **not** snap the controls back to neutral — it leaves ailerons/elevator at their last commanded position, exactly like releasing a physical control axis would. Center your mouse in the square before deactivating if you want a neutral handoff (or just reactivate, since that re-centers automatically).

## Usage

1. Launch MSFS 2024 and get into a flight.
2. Launch `MouseYoke.exe`.
3. Press `Ctrl+Y` (or your configured hotkey) — a small transparent square appears on your primary monitor, and your cursor snaps to its center.
4. Move the mouse inside the square to control ailerons/elevator (watch the green dot track your position).
5. Press the hotkey again to hide the square and get your mouse back for clicking MSFS's own UI/cockpit.

Right-click the tray icon for **Settings** (hotkey, square size, deadzone, response curve, invert axes) and **Exit**. You can launch MouseYoke before or after MSFS - it retries the SimConnect connection every 5 seconds in the background and recovers automatically if the sim restarts.

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

## Building from source

Only needed if you want to modify the code or produce your own distributable - none of this is required just to run the app (see [Download & run](#download--run) above).

**Requirements:**
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft Flight Simulator 2024 (Steam or Microsoft Store/Xbox app — both work, see the Store-specific compatibility note above)
- The free **MSFS 2024 SDK**. Unlike most SDKs, it isn't a standalone web download — it ships from inside the sim itself:
  1. In MSFS, go to **Settings > General > Developer Tools** (or **Advanced Options**, wording varies by build) and switch **Developer Mode** on.
  2. Restart/return to the sim — a **DevMode** menu bar appears across the top.
  3. Open **Help > SDK Installer** — this downloads an `.msi`. Run it; it installs to `C:\MSFS 2024 SDK\` by default and sets an `MSFS_SDK` environment variable that this project's `.csproj` uses to locate `SimConnect.dll`. This is a **build-time only** requirement — it's never needed by someone just running the finished app.
- Visual Studio 2022 (recommended) or just the `dotnet` CLI.

**Dev build/run** (fast inner loop, produces a normal multi-file output under `MouseYoke\bin\...`):
```bash
dotnet restore
dotnet build MouseYoke.sln -c Release
dotnet run --project MouseYoke -c Release
```
If the build fails to find `Microsoft.FlightSimulator.SimConnect`, confirm the `MSFS_SDK` environment variable is set (reopen your terminal/IDE after installing the SDK) and points at a folder containing `SimConnect SDK\lib\managed\Microsoft.FlightSimulator.SimConnect.dll`. If your SDK version lays files out differently, edit the two `HintPath`/`Include` paths in `MouseYoke/MouseYoke.csproj` directly.

**Producing the distributable** (what ends up on the Releases page):
```powershell
.\publish.ps1
```
This runs `dotnet publish` as a self-contained, single-file, win-x64 build and drops the result in `dist\`. It's not a *literal* single file: `Microsoft.FlightSimulator.SimConnect.dll` is a mixed-mode (C++/CLI) assembly, and .NET's single-file publish feature cannot embed mixed-mode assemblies at all — it crashes on startup with a `BadImageFormatException` if forced. The `.csproj` explicitly excludes it (and the native `SimConnect.dll` it wraps) from the bundle via a `ResolvedFileToPublish` target, so they ship as two small loose files next to `MouseYoke.exe` instead. Still just 3 files total, versus the 10+ files (plus a separate .NET runtime install) a normal framework-dependent build would need.

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
publish.ps1                      Builds the distributable, see "Building from source" above
NOTICE.md                        Third-party attribution for the redistributed SimConnect DLLs
```

## A note on testing

This has been built and tested live against MSFS 2024. Aileron and elevator direction were confirmed correct by the user flying with it. The overlay rendering, live indicator dot, and cursor auto-centering on activation have all been verified end-to-end by launching the real exe, injecting input, and screenshotting the result - including the published self-contained `dist\MouseYoke.exe` specifically, not just the dev build. Throttle-via-scroll was implemented, tested, found to permanently conflict with MSFS's own zoom (not fixable client-side), and removed rather than shipped half-working.
