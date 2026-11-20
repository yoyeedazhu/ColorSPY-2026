# ColorSPY 2026

A tiny color picker that lives in the bottom-right corner of your desktop. Most of the time it is just a small tray-style panel; press **Pick color** (or `Ctrl+Alt+C`) to enter full-screen picking mode. The cursor turns into a crosshair, a magnifier follows it, and a click only picks the color — it never reaches the app underneath.

Windows 10 / 11 · .NET Framework 4.8 · single executable · no Flash · offline

English | [简体中文](README.zh-CN.md)

![Tray panel](screenshots/preview-100.png)

![Picking mode](screenshots/preview-pick.png)

## Features

- Docks at the bottom-right of the desktop (above the taskbar) and shows the last picked color as HEX / RGB / HSV.
- Tray icon: double-click to start picking; right-click for Start picking / Show panel / Exit.
- Picking mode: crosshair cursor, an 11×11 pixel magnifier next to the cursor with the center cell highlighted.
- Left click picks the color and copies HEX to the clipboard; right click or `Esc` cancels; mouse wheel zooms; arrow keys nudge by 1 px (`Shift` = 10 px).
- Picking mode covers the whole virtual desktop with a full-screen layer that consumes both mouse-down and mouse-up, so the click cannot trigger a browser, editor or game below.
- Click the swatch to copy HEX; drag the panel to move it and the position is remembered; right-click menu offers always-on-top, copy, hide to tray.
- Multi-monitor, negative coordinates, mixed DPI and monitor hot-plug are supported.

## Usage

1. Run `ColorSPY2026.exe` (read the SmartScreen note below on first launch).
2. The panel appears at the bottom-right. Click **Pick color** or press `Ctrl+Alt+C`.
3. Move the mouse, aim with the magnifier and click. The color is copied automatically.
4. `Esc` hides the panel to the tray; the × button exits.

## Multi-monitor

- The picking layer captures each monitor separately and stitches them together, so a monitor to the left of the primary (negative coordinates) works correctly.
- The magnifier scales with the DPI of the monitor under the cursor; the panel re-lays itself out when moved between 100% and 150% screens.
- After a resolution change or a monitor being plugged/unplugged, the panel is moved back into a visible area.

## Build

Requires the .NET Framework 4.x compiler that ships with Windows (no SDK, no network):

```bat
build.cmd
```

This produces `ColorSPY2026.exe`. The script first builds `tools\IconMaker.cs` to generate the multi-size icon, then compiles the sources under `src`.

GitHub Actions builds on every push and uploads a `ColorSPY2026` artifact; pushing a `v*` tag creates a Release with the executable attached.

## SmartScreen

The executable is not code-signed, so Windows SmartScreen may show "Windows protected your PC" when you double-click it in Explorer.

- Recommended: run `launch.cmd`, which starts the executable through `cmd` and bypasses Explorer's SmartScreen check.
- Or click "More info → Run anyway".
- To remove the warning entirely you need a code-signing certificate from a trusted CA (an EV certificate establishes reputation immediately). A self-signed certificate does not remove SmartScreen.

## Layout

- `src\Program.cs` — entry point, single instance, DPI awareness, crash log
- `src\SpyForm.cs` — bottom-right tray panel, tray icon, menu, hotkey, picking flow
- `src\PickOverlay.cs` — full-screen picking layer (crosshair, magnifier, click interception)
- `src\ColorMath.cs` — HEX / RGB / HSV conversion
- `src\Theme.cs` — colors and drawing helpers
- `src\State.cs` — position, always-on-top and last-color persistence
- `src\app.manifest` — PerMonitorV2 declaration
- `tools\IconMaker.cs` — generates the multi-size `.ico`
- `launch.cmd` — launcher that bypasses SmartScreen

## License

MIT, see [LICENSE](LICENSE).

## Disclaimer

This is an independent reimplementation inspired by classic Windows color-picker utilities. It is not affiliated with the authors or publishers of the original `ColorSPY` or `Quester's ColorKey`. No original binaries are included in this repository.
