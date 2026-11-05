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

- Normal cursor: 40 px
- Click pulse: `40 -> 42 -> 44 -> 46 -> 48 -> 48 -> 46 -> 44 -> 42 -> 40 px`
- Approximately 200 ms per pulse
- High-DPI aware and no administrator rights required

## Features

- Click-through, topmost layered overlay
- Smooth grow-and-return animation on left click
- DWM presentation synchronization prevents the click-start flash
- Tray menu to enable/disable the effect or exit
- Automatically backs up the previous cursor configuration
- One-click install and uninstall
- No network access, telemetry, services, or system-file modifications

## Install

### Recommended: download a release

1. Open the [latest release](https://github.com/MiaomiaoYuHao/PaperPlaneGlowCursor/releases/latest).
2. Download `PaperPlaneGlowCursor-v1.0.0-win.zip`.
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

## How it works

The helper is a small WinForms tray application. It keeps a transparent topmost layered window prewarmed, swaps the system cursor to a transparent cursor during a pulse, and renders the `40–48 px` animation frames at 20 ms intervals. `DwmFlush` synchronizes cursor replacement and overlay presentation to avoid a one-frame flash.

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
├─ PaperPlaneGlow.cur
├─ PaperPlaneGlow-Blank.cur
├─ PaperPlaneGlow-Frame*.png
├─ PaperPlaneGlow.ico
├─ source/
│  ├─ PaperPlaneGlowClick.cs
│  └─ PaperPlaneGlow.svg
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
- Left mouse button only.
- The installed configuration points to the extracted folder; moving it requires reinstalling.
- Changing the Windows cursor scheme later may overwrite the configuration.
- The executable is unsigned, so SmartScreen may warn.

## License

[MIT](LICENSE) © 2026 MiaomiaoYuHao