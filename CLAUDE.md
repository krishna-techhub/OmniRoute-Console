# OmniRoute Console

Native Windows companion launcher and system tray controller for the OmniRoute server (`127.0.0.1:20128`).

## Architecture & Modules

- `src/Program.cs` — STAThread entry point with Win32 Named Mutex (`OmniRouteConsole_SingleInstance_Mutex`), `WM_SHOWME` IPC broadcast, and CLI argument parsing (`--minimized`, `--silent`, `--port <n>`).
- `src/LauncherForm.cs` — FixedDialog UI, system tray `NotifyIcon`, right-click `ContextMenuStrip`, Crash Guardian watchdog, custom directory picker dialog, auto-close timer, hover cancel, server controls.
- `src/ServerManager.cs` — Dynamic multi-path `.omniroute` discovery, registry persistence (`HKCU\Software\OmniRouteConsole`), `.env` dynamic port detection, Windows Run registry auto-start, TCP ping latency check, background update checker.
- `src/UITheme.cs` — Design tokens (Ink, Brand, Muted, Border, Status colors).
- `installer/Program.cs` — Entry point for standalone Windows setup wizard.
- `installer/SetupForm.cs` — Standalone setup GUI with embedded binary extraction, shortcut generation, and shell icon cache refresh.
- `assets/` — Master SVG (`icon.svg`) and multi-resolution PE icon (`omniroute.ico`).
- `dist/` — Standalone distribution binaries (`OmniRouteConsole.exe` & `OmniRouteConsole-Setup.exe`) tracked in git for direct raw downloads.
- `version.txt` — Plaintext version string (`1.0.0`) used by background update checker.
- `index.html` — GitHub Pages product page with interactive tray simulator.
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

Direct Git Bash compilation (without PowerShell):
```bash
MSYS2_ARG_CONV_EXCL="*" /c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe /target:winexe /optimize+ /platform:x64 /win32icon:assets\\omniroute.ico /out:bin\\OmniRouteConsole.exe "src\\Program.cs" "src\\UITheme.cs" "src\\ServerManager.cs" "src\\LauncherForm.cs"
MSYS2_ARG_CONV_EXCL="*" /c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe /target:winexe /optimize+ /platform:x64 /win32icon:assets\\omniroute.ico /resource:bin\\OmniRouteConsole.exe,OmniRouteConsole.exe /resource:assets\\omniroute.ico,omniroute.ico /out:bin\\OmniRouteConsole-Setup.exe "installer\\Program.cs" "installer\\SetupForm.cs"
```

Binary outputs:
- `bin/OmniRouteConsole.exe`
- `bin/OmniRouteConsole-Setup.exe`

Sync binaries to distribution:
```powershell
Copy-Item 'bin\OmniRouteConsole.exe' 'dist\OmniRouteConsole.exe' -Force
Copy-Item 'bin\OmniRouteConsole-Setup.exe' 'dist\OmniRouteConsole-Setup.exe' -Force
```

## Key Constraints & Gotchas

- **Compiler Version:** Compiles via `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (C# 5 syntax; avoid C# 6+ features like expression-bodied members or string interpolation `$"..."`).
- **Icon Embedding:** Always pass `/win32icon:assets\omniroute.ico` during compilation so Explorer displays the icon without external shortcuts.
- **Embedded Resources in Setup:** `OmniRouteConsole-Setup.exe` embeds `OmniRouteConsole.exe` and `omniroute.ico` via `/resource:bin\OmniRouteConsole.exe,OmniRouteConsole.exe`.
- **Single-Instance Mutex:** Second instance launch detects existing mutex, posts registered window message `WM_SHOW_OMNIROUTE_CONSOLE` to bring the active window to foreground, and terminates immediately.
- **Silent Startup (`--minimized`):** When launched with `--minimized` or `--silent` (e.g. via Windows Run registry), `SetVisibleCore(false)` ensures the app starts directly into the system tray without a visual form flicker.
- **Data Directory Priority:** Resolution order: 1) `HKCU\Software\OmniRouteConsole\DataDir`, 2) `OMNIROUTE_HOME` / `OMNIROUTE_DIR`, 3) `%USERPROFILE%\.omniroute`, 4) `%APPDATA%\.omniroute`, 5) `%LOCALAPPDATA%\.omniroute`.
- **Dynamic Port:** Server port defaults to `20128` but is auto-detected from `.env` in the active data directory (`PORT=` or `OMNIROUTE_PORT=`) or overridden via CLI `--port <n>`.
- **System Tray Form Closing:** User closing the form via `X` or `Esc` cancels termination (`e.Cancel = true`) and hides the window to tray. Real exit requires `ExitApplication()` via the context menu.
- **Auto-Update Mechanism:** `ServerManager.CheckUpdateAsync()` fetches `version.txt` from GitHub raw with a 3-second timeout. When bumping versions, update both `LauncherForm.cs` current version string and `version.txt`.
- **Git Bash / MSYS2 Argument Escaping:** Git Bash automatically converts forward-slash switches (`/optimize+`) into Windows file paths (`C:/Program Files/Git/optimize+`). Set `MSYS2_ARG_CONV_EXCL="*"` or execute via `powershell -Command` or `cmd.exe /c`.
- **Explorer Icon Cache:** When shortcuts are added or modified, invoke Windows P/Invoke `SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero)` to force Explorer to refresh cached icons immediately.
- **Detached Execution:** Always execute Node CLI via `cmd.exe /c omniroute serve --tray` with `%APPDATA%\npm` and Node directory prepended to child environment PATH.
- **Port:** Default target port is `20128`.

## Release & Deployment

- Release workflow is defined in `workflows/release.yml`.
- Pushing a version tag (e.g. `v1.0.0`) triggers GitHub Actions to build `OmniRouteConsole.exe`, `OmniRouteConsole-Setup.exe`, create distribution ZIP `OmniRouteConsole-Windows-x64.zip`, and publish a GitHub Release with assets attached.
- Public 1-line installation scripts (`install.ps1`, `install.cmd`, `uninstall.ps1`) are served via `raw.githubusercontent.com`.
