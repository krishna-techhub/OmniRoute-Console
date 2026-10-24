<#
.SYNOPSIS
    OmniRoute Console Uninstaller
.DESCRIPTION
    Safely terminates running processes, removes Desktop & Start Menu shortcuts,
    deletes application files, and refreshes the shell icon cache.
#>

[CmdletBinding()]
param()

Write-Host "Uninstalling OmniRoute Console..." -ForegroundColor Cyan

# 1. Safely terminate running OmniRouteConsole process if open
$runningProcs = Get-Process -Name "OmniRouteConsole" -ErrorAction SilentlyContinue
if ($runningProcs) {
    Write-Host "Closing active OmniRoute Console instance(s)..." -ForegroundColor Yellow
    foreach ($proc in $runningProcs) {
        try {
            $proc.CloseMainWindow() | Out-Null
            Start-Sleep -Milliseconds 500
            if (-not $proc.HasExited) {
                $proc.Kill()
                $proc.WaitForExit(2000)
            }
        } catch {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
    }
}

# 2. Delete Desktop Shortcut
$desktopShortcut = Join-Path $env:USERPROFILE "Desktop\OmniRoute Console.lnk"
if (Test-Path $desktopShortcut) {
    try {
        Remove-Item -Path $desktopShortcut -Force -ErrorAction Stop
        Write-Host "Removed Desktop shortcut: $desktopShortcut" -ForegroundColor DarkGray
    } catch {
        Write-Warning "Could not remove Desktop shortcut: $($_.Exception.Message)"
    }
}

# 3. Delete Start Menu Shortcut
$startMenuShortcut = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\OmniRoute Console.lnk"
if (Test-Path $startMenuShortcut) {
    try {
        Remove-Item -Path $startMenuShortcut -Force -ErrorAction Stop
        Write-Host "Removed Start Menu shortcut: $startMenuShortcut" -ForegroundColor DarkGray
    } catch {
        Write-Warning "Could not remove Start Menu shortcut: $($_.Exception.Message)"
    }
}

# 4. Delete LocalAppData directory
$installDir = Join-Path $env:LOCALAPPDATA "OmniRouteConsole"
if (Test-Path $installDir) {
    try {
        Start-Sleep -Milliseconds 400
        Remove-Item -Path $installDir -Recurse -Force -ErrorAction Stop
        Write-Host "Removed installation directory: $installDir" -ForegroundColor DarkGray
    } catch {
        Write-Warning "Could not remove installation directory ($installDir): $($_.Exception.Message)"
    }
}

# 5. Refresh Windows Explorer icon cache via SHChangeNotify (0x08000000 = SHCNE_ASSOCCHANGED)
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
    Write-Host "Refreshed Windows shell icon cache." -ForegroundColor DarkGray
} catch {
    # Non-fatal if icon refresh encounters an issue
}

Write-Host ""
Write-Host "OmniRoute Console has been completely removed." -ForegroundColor Green
