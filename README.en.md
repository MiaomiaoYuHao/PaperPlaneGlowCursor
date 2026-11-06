<div align="center">

# Paper Plane Glow Cursor

A lightweight glowing paper-plane cursor for Windows with a smooth click pulse.

[简体中文](README.md) | [English](README.en.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4.svg)](#requirements)
[![Latest Release](https://img.shields.io/github/v/release/MiaomiaoYuHao/PaperPlaneGlowCursor?display_name=tag&sort=semver)](https://github.com/MiaomiaoYuHao/PaperPlaneGlowCursor/releases/latest)
[![Build](https://github.com/MiaomiaoYuHao/PaperPlaneGlowCursor/actions/workflows/build.yml/badge.svg)](https://github.com/MiaomiaoYuHao/PaperPlaneGlowCursor/actions/workflows/build.yml)

![Paper Plane Glow Cursor](docs/preview.png)

</div>

## Preview

![Click animation](docs/click-preview.gif)

- Normal cursor: 40 px visual content on a native 48 px canvas
- Click pulse: `40 -> 42 -> 44 -> 46 -> 48 -> 48 -> 46 -> 44 -> 42 -> 40 px`
- Native Windows animated cursor, restored after approximately 200 ms
- Works on the taskbar, system tray, Start menu, and system flyouts
- High-DPI aware and no administrator rights required

## Features

- Native Windows `.ani` animated cursor, with no window overlay
- Works on the taskbar, system tray, Start menu, and system flyouts
- The system cursor remains visible throughout the animation
- Zero tracking lag because Windows renders the cursor natively
- Tray menu to enable/disable the effect or exit
- Automatically backs up the previous cursor configuration
- One-click install and uninstall
- No network access, telemetry, services, or system-file modifications

## Install

### Recommended: download a release

1. Open the [latest release](https://github.com/MiaomiaoYuHao/PaperPlaneGlowCursor/releases/latest).
2. Download `PaperPlaneGlowCursor-v1.1.0-win.zip`.
3. Extract the entire ZIP to a permanent folder, for example:
   `C:\Users\<you>\Apps\PaperPlaneGlowCursor`
4. Double-click `Install.cmd`.
5. The paper-plane tray icon will appear when installation finishes.

> Do not run files directly from the ZIP preview. Keep the extracted folder in place after installation. If you move it, run `Install.cmd` again from the new location.

### PowerShell

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Current.ps1
```

The installer only writes to the current user's registry:

- `HKCU\Control Panel\Cursors`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

It does not request administrator privileges.

> The included executable is not commercially code-signed, so Windows SmartScreen may report an unknown publisher. The complete C# source and build script are included; you can inspect them or rebuild the executable with `build.ps1`.

## Usage

- **Left click**: play the paper-plane grow-and-return pulse.
- **Right-click the tray icon**:
  - `Click Grow: Enabled / Disabled`: toggle the pulse
  - `Exit`: stop the helper and restore the normal cursor
- The pulse only runs while the standard arrow cursor is visible.

## Update

1. Right-click the tray icon and choose `Exit`.
2. Extract the new release over the existing folder.
3. Run `Install.cmd` again.

Keep `previous-cursor-config.json`; it is used to restore the original cursor when uninstalling.

## Uninstall

Double-click `Uninstall.cmd`, or run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Restore-Previous.ps1
```

This stops the helper, removes the startup entry, and restores the cursor configuration captured before installation.

To disable only the click animation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-ClickEffect.ps1
```

## Requirements

- Windows 10 or Windows 11
- 64-bit Windows
- .NET Framework 4.8 (included with Windows 10/11)

macOS, Linux, and Windows 7/8 are not supported.

## Build from source

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

The build uses the C# compiler included with the Windows .NET Framework. Visual Studio and network package restore are not required. The output is written to `dist\PaperPlaneGlowClick.exe`.

To regenerate the `.cur` and `.ani` assets:

```powershell
python -m pip install -r tools\requirements.txt
python tools\generate_cursor_assets.py
```

## How it works

The helper is a small WinForms tray application. The normal state uses a native `.cur` cursor with a 48 px canvas and 40 px visual content. On left click, it loads `PaperPlaneGlowClick.ani` and calls `SetSystemCursor` to switch to the native animated cursor. Windows renders the grow-and-return frames directly, then the helper restores the normal cursor after about 220 ms. Because the animation lives in the system cursor layer, it stays above the taskbar, Start menu, and system flyouts.

## Project layout

```text
.
├─ Install.cmd
├─ Uninstall.cmd
├─ Install-Current.ps1
├─ Restore-Previous.ps1
├─ Uninstall-ClickEffect.ps1
├─ build.ps1
├─ PaperPlaneGlowClick.exe
├─ PaperPlaneGlowClick.ani
├─ PaperPlaneGlow.cur
├─ PaperPlaneGlow-Frame*.png
├─ PaperPlaneGlow.ico
├─ source/
│  ├─ PaperPlaneGlowClick.cs
│  └─ PaperPlaneGlow.svg
├─ tools/
│  ├─ generate_cursor_assets.py
│  └─ requirements.txt
├─ docs/
├─ CHANGELOG.md
└─ LICENSE
```

## Safety and privacy

- No network requests, telemetry, or data collection.
- No service is installed; the helper starts through the current user's `Run` key.
- Other users and system files are not modified.
- All installer, uninstaller, and build logic is available as readable PowerShell/C# source.

## Known limitations

- Windows only, and only for the standard arrow cursor.
- Native cursor animation timing follows the Windows cursor clock (roughly 1/60 second per frame).
- Left mouse button only.
- The installed configuration points to the extracted folder; moving it requires reinstalling.
- Changing the Windows cursor scheme later may overwrite the configuration.
- The executable is unsigned, so SmartScreen may warn.

## License

[MIT](LICENSE) © 2026 MiaomiaoYuHao