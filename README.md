<p align="center">
  <img src="assets/icon.svg" width="128" height="128" alt="OmniRoute Console Logo" />
</p>

<h1 align="center">OmniRoute Console</h1>

<p align="center">
  <strong>Lightweight Windows GUI Manager, System Tray Controller & One-Line Installer for OmniRoute Server</strong>
</p>

<p align="center">
  <a href="https://github.com/krishna-techhub/OmniRoute-Console/releases"><img src="https://img.shields.io/github/v/release/krishna-techhub/OmniRoute-Console?color=7571da&logo=github" alt="Release" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License: MIT" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6?logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET-Framework%204.5%2B-512BD4?logo=dotnet" alt=".NET Framework" />
</p>

---

## ⚡ Quick Install

Install OmniRoute Console in seconds with full Desktop and Start Menu integration.

### ⚡ Quick Install (Windows PowerShell)

```powershell
irm https://is.gd/omni_console | iex
```

*Or via direct GitHub raw link:*

```powershell
irm https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/install.ps1 | iex
```

### 💻 Command Prompt (CMD / Curl)

```cmd
curl -sL https://is.gd/omni_console_cmd -o i.cmd && i.cmd && del i.cmd
```

*Or via direct GitHub raw link:*

```cmd
curl -fsSL https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/install.cmd -o install.cmd && install.cmd && del install.cmd
```

---

## ✨ Features

- 🔕 **Zero Console Windows**: Launches server silently in background with Windows System Tray (`omniroute serve --tray`).
- ⚡ **Instant Status Detection**: Asynchronous non-blocking loopback TCP check detects whether server is already running in <50ms.
- 🔄 **One-Click Restart & Toggle**: Safely stop, start, or restart without leaving orphan processes.
- 🌐 **Instant Dashboard**: Launch browser directly to `http://localhost:20128/home`.
- 🎨 **Multi-Resolution Native Icon**: Crisp display on all DPI scaling settings (100% to 200%).
- ⏱️ **Smart Auto-Close**: Closes in 5 seconds if server is healthy, immediately pauses on mouse hover.

---

## 🖥️ Usage & Controls

When launched, OmniRoute Console automatically inspects whether the OmniRoute server is active on `127.0.0.1:20128`:

| Control | Action |
|---|---|
| **Dashboard** | Opens default web browser directly to `http://localhost:20128/home`. |
| **Restart** | Safely terminates any active instance, waits for port release, and restarts silently. |
| **Stop / Start** | Dynamic toggle to manually start the background server or cleanly terminate it. |
| **Close** | Dismisses the launcher dialog (background server and tray icon continue running). |

*Note: If the server is already active and healthy when OmniRoute Console opens, a 5-second auto-close countdown starts automatically. Moving or hovering your mouse over the window immediately cancels the timer.*

---

## 🛠️ Manual Build Instructions

OmniRoute Console compiles with zero external dependencies using the Windows built-in C# compiler (`csc.exe`):

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The script builds `OmniRouteConsole.exe` and embeds the high-resolution multi-size icon (`assets/omniroute.ico`).

---

## 🏛️ Architecture & Tech Stack

- **Pure Native C#**: Compiled with the Windows built-in `csc.exe` targeting .NET Framework 4.5+.
- **Zero Node.js Runtime for Launcher**: The companion launcher is completely native (~200 KB) and requires no Node.js or npm runtime to execute.
- **Asynchronous TCP Loopback Checking**: Probes port `20128` asynchronously without UI thread stalls.
- **Clean Process Management**: Spawns detached processes (`omniroute serve --tray`) and avoids orphan console windows.

---

## 🗑️ Uninstallation

To completely remove OmniRoute Console, application binaries, and all created shortcuts:

```powershell
irm https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/uninstall.ps1 | iex
```

---

## 📄 License & Author

- **Author**: [krishna-techhub](https://github.com/krishna-techhub)
- **License**: [MIT](LICENSE)
