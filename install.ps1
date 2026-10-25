<#
.SYNOPSIS
    OmniRoute Console 1-Line Installer
.DESCRIPTION
    Installs OmniRoute Console to $env:LOCALAPPDATA\OmniRouteConsole,
    configures Desktop and Start Menu shortcuts, and refreshes icon cache.
#>

[CmdletBinding()]
param(
    [switch]$NoLaunch
)

# 1. Platform verification
if ($PSVersionTable.PSEdition -eq "Core" -and -not $IsWindows) {
    Write-Error "OmniRoute Console requires Windows."
    return
}
if ([System.Environment]::OSVersion.Platform -ne [System.PlatformID]::Win32NT) {
    Write-Error "OmniRoute Console is only supported on Windows NT (Windows 10/11)."
    return
}

# Ensure TLS 1.2 is enabled for downloads
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12

# Professional ASCII Banner
$banner = @"

   ____                  _ ____             _          ____                      _
  / __ \____ ___  ____  (_) __ \____  __  _| |_ ___   / ________  ____  _________  / ___
 / / / / __ `__ \/ __ \/ / /_/ / __ \/ / / / __/ _ \ / /   / __ \/ __ \/ ___/ __ \/ / _ \
/ /_/ / / / / / / / / / / _, _/ /_/ / /_/ / /_/  __// /___/ /_/ / / / (__  ) /_/ / /  __/
\____/_/ /_/ /_/_/ /_/_/_/ |_|\____/\__,_/\__/\___/ \____/\____/_/ /_/____/\____/_/\___/

                       Native Windows Management Console
"@

Write-Host $banner -ForegroundColor Cyan

# 2. Target directory setup
$installDir = Join-Path $env:LOCALAPPDATA "OmniRouteConsole"
if (-not (Test-Path -Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

$targetExe = Join-Path $installDir "OmniRouteConsole.exe"
$targetIco = Join-Path $installDir "omniroute.ico"

Write-Host "================================================================================" -ForegroundColor DarkGray
Write-Host " Install Target : " -NoNewline -ForegroundColor Gray
Write-Host $installDir -ForegroundColor White
Write-Host "================================================================================" -ForegroundColor DarkGray
Write-Host ""

# Safely close running instance if active during installation/upgrade
$runningProcs = Get-Process -Name "OmniRouteConsole" -ErrorAction SilentlyContinue
if ($runningProcs) {
    Write-Host "Closing active OmniRoute Console instance for update..." -ForegroundColor Yellow
    foreach ($proc in $runningProcs) {
        try {
            $proc.CloseMainWindow() | Out-Null
            Start-Sleep -Milliseconds 400
            if (-not $proc.HasExited) { $proc.Kill() }
        } catch {}
    }
}

# 3. Procure icon asset
Write-Host "[1/4] Procuring application icon..." -ForegroundColor Cyan
$iconUrl = "https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/assets/omniroute.ico"
$iconObtained = $false

try {
    Invoke-WebRequest -Uri $iconUrl -OutFile $targetIco -UseBasicParsing -TimeoutSec 15
    if ((Test-Path $targetIco) -and ((Get-Item $targetIco).Length -gt 0)) {
        $iconObtained = $true
        Write-Host "      Icon downloaded from GitHub." -ForegroundColor DarkGray
    }
} catch {
    # Fallback to local file if available
}

if (-not $iconObtained) {
    $localIco = $null
    if ($PSScriptRoot -and (Test-Path (Join-Path $PSScriptRoot "assets\omniroute.ico"))) {
        $localIco = Join-Path $PSScriptRoot "assets\omniroute.ico"
    } elseif (Test-Path "assets\omniroute.ico") {
        $localIco = (Resolve-Path "assets\omniroute.ico").Path
    }

    if ($localIco -and (Test-Path $localIco)) {
        Copy-Item -Path $localIco -Destination $targetIco -Force
        Write-Host "      Copied icon from local repository assets." -ForegroundColor DarkGray
    } else {
        Write-Warning "Application icon not available; shortcut will use default system icon."
    }
}

# 4. Procure executable binary
Write-Host "[2/4] Procuring OmniRoute Console executable..." -ForegroundColor Cyan
$binaryObtained = $false

$downloadUrls = @(
    "https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/dist/OmniRouteConsole.exe",
    "https://github.com/krishna-techhub/OmniRoute-Console/releases/latest/download/OmniRouteConsole.exe"
)

foreach ($url in $downloadUrls) {
    if ($binaryObtained) { break }
    try {
        Write-Host "      Attempting download from $url..." -ForegroundColor DarkGray
        Invoke-WebRequest -Uri $url -OutFile $targetExe -UseBasicParsing -TimeoutSec 15
        if ((Test-Path $targetExe) -and ((Get-Item $targetExe).Length -gt 2048)) {
            $binaryObtained = $true
            Write-Host "      Pre-built binary downloaded successfully." -ForegroundColor Green
            break
        } else {
            if (Test-Path $targetExe) { Remove-Item $targetExe -Force -ErrorAction SilentlyContinue }
        }
    } catch {
        Write-Host "      Download attempt failed ($($_.Exception.Message))." -ForegroundColor DarkGray
        if (Test-Path $targetExe) { Remove-Item $targetExe -Force -ErrorAction SilentlyContinue }
    }
}

# Fallback 1: Check local build output
if (-not $binaryObtained) {
    $localBin = $null
    if ($PSScriptRoot -and (Test-Path (Join-Path $PSScriptRoot "dist\OmniRouteConsole.exe"))) {
        $localBin = Join-Path $PSScriptRoot "dist\OmniRouteConsole.exe"
    } elseif ($PSScriptRoot -and (Test-Path (Join-Path $PSScriptRoot "bin\OmniRouteConsole.exe"))) {
        $localBin = Join-Path $PSScriptRoot "bin\OmniRouteConsole.exe"
    } elseif (Test-Path "dist\OmniRouteConsole.exe") {
        $localBin = (Resolve-Path "dist\OmniRouteConsole.exe").Path
    } elseif (Test-Path "bin\OmniRouteConsole.exe") {
        $localBin = (Resolve-Path "bin\OmniRouteConsole.exe").Path
    }

    if ($localBin -and (Test-Path $localBin)) {
        Write-Host "      Copying locally built binary ($localBin)..." -ForegroundColor Cyan
        Copy-Item -Path $localBin -Destination $targetExe -Force
        $binaryObtained = $true
    }
}

# Fallback 2: Compile on-the-fly with native .NET csc.exe (fetching sources if needed)
if (-not $binaryObtained) {
    Write-Host "      Attempting compilation with native Windows .NET csc.exe..." -ForegroundColor Cyan
    $cscCandidates = @(
        "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    )
    $cscExe = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($cscExe) {
        $tempBuildDir = Join-Path $env:TEMP "OmniRouteConsole-Build-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
        New-Item -ItemType Directory -Path $tempBuildDir -Force | Out-Null
        try {
            $sourceFiles = @("Program.cs", "LauncherForm.cs", "ServerManager.cs", "UITheme.cs")
            $downloadedSources = @()
            $srcBaseUrl = "https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/src"

            # Check if local src directory exists first
            $localSrc = $null
            if ($PSScriptRoot -and (Test-Path (Join-Path $PSScriptRoot "src"))) {
                $localSrc = Join-Path $PSScriptRoot "src"
            } elseif (Test-Path "src") {
                $localSrc = (Resolve-Path "src").Path
            }

            foreach ($file in $sourceFiles) {
                $destFile = Join-Path $tempBuildDir $file
                if ($localSrc -and (Test-Path (Join-Path $localSrc $file))) {
                    Copy-Item -Path (Join-Path $localSrc $file) -Destination $destFile -Force
                    $downloadedSources += $destFile
                } else {
                    $fileUrl = "$srcBaseUrl/$file"
                    Invoke-WebRequest -Uri $fileUrl -OutFile $destFile -UseBasicParsing -TimeoutSec 10
                    if (Test-Path $destFile) {
                        $downloadedSources += $destFile
                    }
                }
            }

            if ($downloadedSources.Count -eq $sourceFiles.Count) {
                $compileArgs = @(
                    "/target:winexe",
                    "/platform:x64",
                    "/optimize+",
                    "/out:`"$targetExe`""
                )
                if (Test-Path $targetIco) {
                    $compileArgs += "/win32icon:`"$targetIco`""
                }
                $compileArgs += ($downloadedSources | ForEach-Object { "`"$_`"" })

                $cscProcess = Start-Process -FilePath $cscExe -ArgumentList ($compileArgs -join " ") -Wait -NoNewWindow -PassThru
                if ($cscProcess.ExitCode -eq 0 -and (Test-Path $targetExe)) {
                    $binaryObtained = $true
                    Write-Host "      Compiled OmniRouteConsole.exe successfully on this machine." -ForegroundColor Green
                } else {
                    Write-Warning "Compilation failed with exit code $($cscProcess.ExitCode)."
                }
            } else {
                Write-Warning "Could not fetch all source files from GitHub."
            }
        } catch {
            Write-Warning "On-the-fly compilation failed: $($_.Exception.Message)"
        } finally {
            if (Test-Path $tempBuildDir) {
                Remove-Item -Path $tempBuildDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    } else {
        Write-Warning "Native csc.exe compiler not found."
    }
}

if (-not $binaryObtained -or -not (Test-Path $targetExe)) {
    Write-Error "Failed to install OmniRoute Console: Binary could not be downloaded or compiled."
    return
}

# 5. Create desktop and start menu shortcuts
Write-Host "[3/4] Creating shortcuts..." -ForegroundColor Cyan
try {
    $wshShell = New-Object -ComObject WScript.Shell

    # Desktop Shortcut
    $desktopShortcutPath = Join-Path $env:USERPROFILE "Desktop\OmniRoute Console.lnk"
    $desktopShortcut = $wshShell.CreateShortcut($desktopShortcutPath)
    $desktopShortcut.TargetPath = $targetExe
    $desktopShortcut.WorkingDirectory = $installDir
    $desktopShortcut.Description = "OmniRoute Console - Native Windows Manager for OmniRoute"
    if (Test-Path $targetIco) {
        $desktopShortcut.IconLocation = "$targetIco,0"
    } else {
        $desktopShortcut.IconLocation = "$targetExe,0"
    }
    $desktopShortcut.Save()
    Write-Host "      Desktop: $desktopShortcutPath" -ForegroundColor DarkGray

    # Start Menu Shortcut
    $startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
    if (-not (Test-Path $startMenuDir)) {
        New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
    }
    $startMenuShortcutPath = Join-Path $startMenuDir "OmniRoute Console.lnk"
    $startMenuShortcut = $wshShell.CreateShortcut($startMenuShortcutPath)
    $startMenuShortcut.TargetPath = $targetExe
    $startMenuShortcut.WorkingDirectory = $installDir
    $startMenuShortcut.Description = "OmniRoute Console - Native Windows Manager for OmniRoute"
    if (Test-Path $targetIco) {
        $startMenuShortcut.IconLocation = "$targetIco,0"
    } else {
        $startMenuShortcut.IconLocation = "$targetExe,0"
    }
    $startMenuShortcut.Save()
    Write-Host "      Start Menu: $startMenuShortcutPath" -ForegroundColor DarkGray
} catch {
    Write-Warning "Failed to create shortcuts: $($_.Exception.Message)"
}

# 6. Refresh Windows Explorer icon cache
Write-Host "[4/4] Refreshing Windows shell icon cache..." -ForegroundColor Cyan
try {
    if (-not ([System.Management.Automation.PSTypeName]'OmniRoute.NativeMethods').Type) {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
namespace OmniRoute {
    public static class NativeMethods {
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
'@
    }
    [OmniRoute.NativeMethods]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)
    Write-Host "      Shell cache refreshed." -ForegroundColor DarkGray
} catch {
    Write-Warning "Could not invoke SHChangeNotify: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "OmniRoute Console installed successfully!" -ForegroundColor Green

# 7. Launch application
if (-not $NoLaunch) {
    Write-Host "Launching OmniRoute Console..." -ForegroundColor Cyan
    Start-Process -FilePath $targetExe -WorkingDirectory $installDir
}
