# MouseYoke

Fly Microsoft Flight Simulator 2024 with your mouse. MouseYoke brings back FSX's classic "mouse as yoke" control scheme: press a hotkey, a small target appears on your screen, and moving your mouse inside it flies the aircraft's ailerons and elevator — no joystick required.

## Download & run

1. Download `MouseYoke-vX.Y.Z-win-x64.zip` from the [Releases page](https://github.com/seikomogo/FSYoke/releases/latest) and extract it. Keep the three files it contains — `MouseYoke.exe` and two small support DLLs — together in the same folder.
2. Double-click `MouseYoke.exe`. Look for the joystick icon in your system tray (it may be tucked under the little "^" overflow arrow the first time).
3. Launch MSFS 2024, get into a flight, and press `Ctrl+Y`.

That's it — no installer, no .NET download, no extra setup.

## How to use it

1. Launch MSFS 2024 and get into a flight.
2. Launch `MouseYoke.exe`.
3. Press `Ctrl+Y` (or your own hotkey, set in Settings) — a small transparent square appears on screen and your cursor snaps to its center.
4. Move your mouse inside the square to fly: dead center is neutral, the edges are full aileron/elevator deflection. A dot tracks your position live so you always know where you are.
5. Press the hotkey again to hide the square and get your mouse back for MSFS's own menus and cockpit clicks.

Right-click the tray icon any time for **Settings** (hotkey, square size, sensitivity, invert-axis options) or to **Exit**. You can launch MouseYoke before or after MSFS — it connects automatically and recovers if the sim restarts.

## Good to know

- Rudder and throttle aren't included. MSFS's own scroll-to-zoom made a scroll-wheel throttle unworkable, and FSX's original mouse yoke didn't have rudder either.
- Every time you activate the yoke, it automatically centers your cursor and resets ailerons/elevator to neutral, so you always start from a clean, predictable state.
- Works with the default aircraft fleet and the great majority of payware addons without any extra setup.

## Troubleshooting

- **Won't connect to MSFS**: make sure MSFS is running and you're in a flight. If MSFS is running as Administrator, try running MouseYoke as Administrator too.
- **Still won't connect (Microsoft Store/Xbox install)**: create a file named `SimConnect.cfg` next to `MouseYoke.exe` containing:
  ```ini
  [SimConnect]
  Protocol=IPv4
  Address=127.0.0.1
  Port=500
  ```
- **A payware aircraft feels slightly off**: a few complex addons handle control input in unusual ways — the same thing you'd see using a physical joystick axis on that aircraft.
- **Multiple monitors**: the square always appears on your primary display.
- **Using a real joystick/yoke at the same time**: expect the two to fight each other on the same axis, same as plugging in two physical controllers.

---

See [NOTICE.md](NOTICE.md) for third-party licensing information.
