# OmniRoute Console

Native Windows companion launcher and system tray controller for the OmniRoute server (`127.0.0.1:20128`).

## Architecture & Modules

- `src/Program.cs` — STAThread entry point with Windows visual styles.
- `src/LauncherForm.cs` — FixedDialog light UI, auto-close timer, hover cancel, server controls.
- `src/ServerManager.cs` — Non-blocking TCP loopback health check (350ms timeout), detached background tray execution (`omniroute serve --tray`).
- `src/UITheme.cs` — Design tokens (Ink, Brand, Muted, Border, Status colors).
- `installer/Program.cs` — Entry point for standalone Windows setup wizard.
- `installer/SetupForm.cs` — Standalone setup GUI with embedded binary extraction, shortcut generation, and shell icon cache refresh.
- `assets/` — Master SVG (`icon.svg`) and multi-resolution PE icon (`omniroute.ico`).
- `install.ps1` & `install.cmd` — 1-line public installers for Windows.
- `uninstall.ps1` — Clean uninstaller script.

## Build Commands

Compile standalone application and installer with embedded multi-res icon using native Windows compiler:

```powershell
# 1. Local app build using native csc.exe (zero external dependencies)
powershell -ExecutionPolicy Bypass -File .\build.ps1

# 2. Local standalone Setup Wizard build (embeds app & icon inside Setup.exe)
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
```

Binary outputs:
- `bin/OmniRouteConsole.exe`
- `bin/OmniRouteConsole-Setup.exe`

## Key Constraints & Gotchas

- **Compiler Version:** Compiles via `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (C# 5 syntax; avoid C# 6+ features like expression-bodied members or string interpolation `$"..."`).
- **Icon Embedding:** Always pass `/win32icon:assets/omniroute.ico` during compilation so Explorer displays the icon without external shortcuts.
- **Detached Execution:** Always execute Node CLI via `cmd.exe /c omniroute serve --tray` with `%APPDATA%\npm` and Node directory prepended to child environment PATH.
- **Port:** Default target port is `20128`.
