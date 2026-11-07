# Changelog

All notable changes to this project are documented here.

## v1.1.2 - 2026-11-07

- Added a Startup folder shortcut alongside the per-user Run entry for more reliable login auto-start.
- The installer now verifies that the Run entry was written successfully.
- Uninstall and restore scripts now remove both startup registrations.
- Added an auto-start troubleshooting section to the README.

## v1.1.1 - 2026-11-06

- Fixed a regression where `CopyIcon` flattened the `.ani` cursor to a static frame.
- Each pulse now loads a fresh file-backed animated cursor handle directly, restoring the verified native animation behavior.
## v1.1.0 - 2026-11-06

- Replaced the layered overlay renderer with a native Windows animated cursor (`.ani`).
- Click animation now works on the taskbar, system tray, Start menu, and system flyouts.
- The system cursor remains visible for the entire animation instead of being replaced with a transparent cursor.
- Removed cursor position tracking and the overlay z-order dependency.
- Switched to a 48 px native cursor canvas with 40 px visual content.
- Added a reproducible cursor asset generator.

## v1.0.0 - 2026-11-05

- Initial public release.
- Added a 40 px paper-plane cursor with a blue glow.
- Added a smooth click pulse: 40 -> 42 -> 44 -> 46 -> 48 -> 48 -> 46 -> 44 -> 42 -> 40 px.
- Added a tray helper with enable/disable and exit controls.
- Added DWM presentation synchronization to prevent the start-of-click cursor flash.
- Added one-click install and uninstall scripts.
- Added automatic backup and restoration of the previous cursor configuration.
- Added a reproducible build script and Windows CI build.