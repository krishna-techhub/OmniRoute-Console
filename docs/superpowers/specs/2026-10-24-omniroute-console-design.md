# OmniRoute Console - Specification & Architecture Design

**Author:** krishna-techhub  
**Date:** 2026-10-24  
**Project:** OmniRoute Console  
**Repository:** https://github.com/krishna-techhub/OmniRoute-Console  
**Short Install Link:** `irm https://is.gd/uuZr5W | iex`

---

## 1. Executive Summary

OmniRoute Console is a modern, lightweight Windows companion launcher and system-tray controller for the OmniRoute local AI router server (`127.0.0.1:20128`). It solves the problem of unwanted black console windows, invisible crash states, and complex manual command-line startup by providing:
- Zero-terminal background execution with native Windows System Tray presence (`omniroute serve --tray`).
- Millisecond-fast local TCP loopback health checking.
- Clean Windows native light dialog with Dashboard, Restart, and Start/Stop toggle controls.
- Single-line public distribution for all Windows users via PowerShell (`irm https://is.gd/uuZr5W | iex`) and CMD (`curl ...`).
- High-definition multi-resolution icon embedded directly into the PE executable binary.

---

## 2. Directory Structure

```text
omni console/
├── .github/
│   └── workflows/
│       └── release.yml          # Automated CI/CD: tag trigger -> csc/dotnet build -> GitHub Release
├── docs/
│   └── superpowers/
│       └── specs/
│           └── 2026-10-24-omniroute-console-design.md
├── src/
│   ├── Program.cs               # STAThread entry point, VisualStyles, DPI awareness
│   ├── LauncherForm.cs          # FixedDialog UI, hover cancellation, auto-close timer
│   ├── ServerManager.cs         # TCP health probe, detached start (--tray), clean stop
│   └── UITheme.cs               # Color tokens (Ink, Brand, Muted, Border), Button helpers
├── assets/
│   ├── icon.svg                 # Master vector icon
│   └── omniroute.ico            # Multi-res PE icon (16, 24, 32, 48, 64, 128, 256 px)
├── build.ps1                    # One-click local compilation script using native csc.exe
├── install.ps1                  # 1-Line installer (deployed to repo root for short URL)
├── install.cmd                  # CMD / Batch fallback installer
├── uninstall.ps1                # Clean uninstaller script
├── .gitignore
├── LICENSE                      # MIT License
└── README.md                    # Public documentation with badges, copy-paste commands, guide
```

---

## 3. Component Architecture

### 3.1 Core Application (`src/`)
- **`Program.cs`**:
  Initializes `Application.EnableVisualStyles()`, sets `CompatibleTextRenderingDefault(false)`, and runs `LauncherForm`.
- **`LauncherForm.cs`**:
  - Light native Windows dialog (`500 x 205 px`, `FixedDialog`, `TopMost = true`).
  - Asynchronously probes `127.0.0.1:20128` without blocking the UI thread.
  - Controls:
    - **Dashboard**: Opens `http://localhost:20128/home` via default web browser.
    - **Restart**: Safely stops existing server, waits up to 5s for release, then starts `omniroute serve --tray`.
    - **Stop / Start Toggle**: Dynamically toggles server running status with live badge updates.
    - **Close**: Includes a 5-second countdown timer if server is running, which immediately cancels upon mouse movement.
- **`ServerManager.cs`**:
  - `CheckServerHealthy(int port)`: Non-blocking async TCP connect probe (`timeout: 350ms`).
  - `StartServerProcess()`: Detached background execution via `cmd.exe /c omniroute serve --tray` with environment PATH enrichment (`%APPDATA%\npm`, Node.js folder).
  - `StopServerProcess()`: Detached invocation of `omniroute stop`.

---

## 4. Distribution & Installation Pipeline

### 4.1 One-Line PowerShell Installation
```powershell
irm https://is.gd/uuZr5W | iex
```
**Mechanism:**
1. Verifies Windows environment.
2. Target directory: `$env:LOCALAPPDATA\OmniRouteConsole`.
3. Downloads pre-compiled `OmniRouteConsole.exe` and `omniroute.ico`.
4. Creates high-res Desktop Shortcut: `OmniRoute Console.lnk` (pointing to `OmniRouteConsole.exe` with icon index 0).
5. Creates Start Menu Shortcut.
6. Invokes `SHChangeNotify` (`0x08000000`) to instantly refresh the Windows shell icon cache.
7. Prompts user to start OmniRoute Console immediately.

### 4.2 One-Line CMD Installation
```cmd
curl -fsSL https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/install.cmd -o install.cmd && install.cmd && del install.cmd
```

### 4.3 GitHub Actions Release Automation (`.github/workflows/release.yml`)
- Trigger: Tag push (`v*`).
- Environment: `windows-latest`.
- Compiles `OmniRouteConsole.exe` using `csc.exe` / .NET with embedded `omniroute.ico`.
- Uploads `OmniRouteConsole.exe` and `OmniRouteConsole.zip` directly to GitHub Releases.

---

## 5. Verification & Testing Strategy
- Compile test via `build.ps1` using native `csc.exe`.
- Verify PE resource embedding: confirm `omniroute.ico` is present in executable resources.
- Run `OmniRouteConsole.exe`: verify TCP connect detection against running OmniRoute port 20128.
- Test `install.ps1` in dry-run and local install mode.
- Git commit & push to `https://github.com/krishna-techhub/OmniRoute-Console.git`.
