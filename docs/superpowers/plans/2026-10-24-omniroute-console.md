# OmniRoute Console Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build and release OmniRoute Console — a lightweight Windows GUI manager, background tray runner, and 1-line installer pipeline for the OmniRoute server, published to GitHub.

**Architecture:** A native Windows C# application built using the ubiquitous .NET runtime and `csc.exe` with zero external dependencies. The launcher embeds a crisp multi-resolution PE icon, controls the local server via non-blocking TCP loopback probes, starts OmniRoute in detached tray mode (`omniroute serve --tray`), and distributes via a single-line PowerShell command (`irm https://is.gd/omni_console | iex`) and GitHub Actions releases.

**Tech Stack:** C# (WinForms, System.Net.Sockets, System.Diagnostics, System.Drawing), Windows `csc.exe` (v4.0.30319), PowerShell 5.1+, Windows Shell API (`SHChangeNotify`), GitHub Actions CI/CD.

**Spec:** [docs/superpowers/specs/2026-10-24-omniroute-console-design.md](../specs/2026-10-24-omniroute-console-design.md)

## Global Constraints
- Target platform: Windows 10/11 x64.
- Runtime dependency: Zero third-party runtimes. Must compile via standard Windows built-in `csc.exe` or .NET Framework 4.5+.
- OmniRoute server port: Default `20128` on `127.0.0.1`.
- Target install folder: `$env:LOCALAPPDATA\OmniRouteConsole`.
- Short installer link: `https://is.gd/omni_console` pointing to `https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/install.ps1`.
- Repository URL: `https://github.com/krishna-techhub/OmniRoute-Console.git`.

---

### Task 1: Assets & Base Repository Setup

**Files:**
- Create: `assets/icon.svg`
- Create: `assets/omniroute.ico`
- Create: `.gitignore`
- Create: `LICENSE`

**Interfaces:**
- Produces: `assets/omniroute.ico` with 16x16, 24x24, 32x32, 48x48, 64x64, 128x128, 256x256 resolutions for compiler flag `/win32icon`.

- [ ] **Step 1: Write `.gitignore` and `LICENSE` (MIT)**
- [ ] **Step 2: Copy multi-resolution `omniroute.ico` and source `icon.svg` into `assets/`**
- [ ] **Step 3: Verify `.ico` header format contains multi-res images**
- [ ] **Step 4: Commit Task 1 files**
  ```bash
  git add .gitignore LICENSE assets/
  git commit -m "chore: setup repo base, MIT license, and multi-resolution icons"
  ```

---

### Task 2: Core Source Code Modularization

**Files:**
- Create: `src/UITheme.cs`
- Create: `src/ServerManager.cs`
- Create: `src/LauncherForm.cs`
- Create: `src/Program.cs`

**Interfaces:**
- `UITheme`: Provides palette colors (`Ink`, `Muted`, `Brand`, `Border`, `SuccessBg`, `ErrorBg`) and button styling helper `CreateButton(...)`.
- `ServerManager`:
  - `static bool CheckServerHealthy(int port)` -> non-blocking loopback TCP probe.
  - `static void StartServerProcess()` -> executes `cmd.exe /c omniroute serve --tray` detached with enriched PATH.
  - `static void StopServerProcess()` -> executes `omniroute stop`.
  - `static void OpenDashboard()` -> launches `http://localhost:20128/home`.
- `LauncherForm`: WinForms GUI consuming `UITheme` and `ServerManager`.
- `Program`: STAThread entry point.

- [ ] **Step 1: Implement `src/UITheme.cs`**
- [ ] **Step 2: Implement `src/ServerManager.cs`**
- [ ] **Step 3: Implement `src/LauncherForm.cs`**
- [ ] **Step 4: Implement `src/Program.cs`**
- [ ] **Step 5: Verify source syntax and references**
- [ ] **Step 6: Commit Task 2 files**
  ```bash
  git add src/
  git commit -m "feat: implement modular C# launcher UI and server management engine"
  ```

---

### Task 3: Local Build & Compilation Automation

**Files:**
- Create: `build.ps1`
- Output: `bin/OmniRouteConsole.exe`

**Interfaces:**
- `build.ps1`: Invokes `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` with flags:
  `/target:winexe /optimize+ /platform:x64 /win32icon:assets/omniroute.ico /out:bin/OmniRouteConsole.exe src/*.cs`.

- [ ] **Step 1: Create `build.ps1` script with automated path resolution for `csc.exe`**
- [ ] **Step 2: Run `build.ps1` and verify build success**
- [ ] **Step 3: Verify `bin/OmniRouteConsole.exe` extracts correct embedded icon and executes cleanly**
- [ ] **Step 4: Commit Task 3 files**
  ```bash
  git add build.ps1
  git commit -m "build: add automated C# compilation script using native csc.exe"
  ```

---

### Task 4: Public 1-Line Installers & Uninstaller

**Files:**
- Create: `install.ps1`
- Create: `install.cmd`
- Create: `uninstall.ps1`

**Interfaces:**
- `install.ps1`:
  - Resolves install dir `$env:LOCALAPPDATA\OmniRouteConsole`.
  - Downloads latest release binary from GitHub raw/releases.
  - Generates desktop shortcut `OmniRoute Console.lnk` and Start Menu shortcut.
  - Calls `SHChangeNotify` (`0x08000000`) for Explorer icon refresh.
  - Optionally launches app on completion.
- `install.cmd`:
  - Curl wrapper that bootstraps `install.ps1` via `powershell.exe -ExecutionPolicy Bypass`.
- `uninstall.ps1`:
  - Removes shortcuts, cleans `$env:LOCALAPPDATA\OmniRouteConsole`, refreshes icon cache.

- [ ] **Step 1: Implement `install.ps1` with progress indicators and error handling**
- [ ] **Step 2: Implement `install.cmd` for CMD/batch execution**
- [ ] **Step 3: Implement `uninstall.ps1`**
- [ ] **Step 4: Dry-run installer validation**
- [ ] **Step 5: Commit Task 4 files**
  ```bash
  git add install.ps1 install.cmd uninstall.ps1
  git commit -m "feat: add 1-line PowerShell/CMD installers and clean uninstaller"
  ```

---

### Task 5: GitHub Actions CI/CD Release Pipeline

**Files:**
- Create: `.github/workflows/release.yml`

**Interfaces:**
- Triggered on tag push (`v*`).
- Runs on `windows-latest`.
- Compiles `OmniRouteConsole.exe` with embedded `omniroute.ico`.
- Creates a GitHub Release with `OmniRouteConsole.exe` attached as an asset.

- [ ] **Step 1: Create `.github/workflows/release.yml`**
- [ ] **Step 2: Verify YAML syntax**
- [ ] **Step 3: Commit Task 5 files**
  ```bash
  git add .github/
  git commit -m "ci: add GitHub Actions workflow for automated binary releases"
  ```

---

### Task 6: Documentation & Badges

**Files:**
- Create: `README.md`

**Interfaces:**
- Shows badges (GitHub Release, License MIT, Platform Windows).
- Prominently showcases the 1-Line install command:
  ```powershell
  irm https://is.gd/omni_console | iex
  ```
- CMD alternative:
  ```cmd
  curl -fsSL https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/install.cmd -o install.cmd && install.cmd && del install.cmd
  ```
- Features, usage, keyboard shortcuts, tray integration guide, and uninstallation steps.

- [ ] **Step 1: Write comprehensive, modern `README.md`**
- [ ] **Step 2: Verify all markdown links and copy-paste commands**
- [ ] **Step 3: Commit Task 6 files**
  ```bash
  git add README.md
  git commit -m "docs: add comprehensive README with 1-line installation commands"
  ```

---

### Task 7: Verification & Push to GitHub

- [ ] **Step 1: Run full local build test**
- [ ] **Step 2: Verify git status and commit history**
- [ ] **Step 3: Push `main` branch to remote `origin`**
  ```bash
  git push -u origin main
  ```
- [ ] **Step 4: Verify repository state on GitHub**
