# OmniRoute Console - Standalone Installer Build Script
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$binDir = Join-Path $root "bin"
$assetsDir = Join-Path $root "assets"
$installerDir = Join-Path $root "installer"

$ico = Join-Path $assetsDir "omniroute.ico"
$appExe = Join-Path $binDir "OmniRouteConsole.exe"
$setupExe = Join-Path $binDir "OmniRouteConsole-Setup.exe"

# 1. Build main application executable first
Write-Host "Step 1: Building main application executable..." -ForegroundColor Cyan
& (Join-Path $root "build.ps1")

if (-not (Test-Path $appExe)) {
    Write-Error "Failed to build main executable: $appExe"
    exit 1
}

# 2. Locate csc.exe compiler
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path $csc)) {
    Write-Error "csc.exe compiler not found on this system."
    exit 1
}

# 3. Compile standalone Setup Wizard with embedded binary and icon resources
Write-Host "Step 2: Compiling standalone Setup Wizard executable..." -ForegroundColor Cyan

$sources = @(
    (Join-Path $installerDir "Program.cs"),
    (Join-Path $installerDir "SetupForm.cs")
)

foreach ($s in $sources) {
    if (-not (Test-Path $s)) {
        Write-Error "Missing installer source file: $s"
        exit 1
    }
}

$compileArgs = @(
    "/target:winexe",
    "/optimize+",
    "/platform:x64",
    "/win32icon:`"$ico`"",
    "/resource:`"$appExe`",OmniRouteConsole.exe",
    "/resource:`"$ico`",omniroute.ico",
    "/out:`"$setupExe`""
) + ($sources | ForEach-Object { "`"$_`"" })

Write-Host "Invoking $csc with embedded payload..." -ForegroundColor Gray
$process = Start-Process -FilePath $csc -ArgumentList ($compileArgs -join " ") -NoNewWindow -Wait -PassThru

if ($process.ExitCode -eq 0 -and (Test-Path $setupExe)) {
    $size = (Get-Item $setupExe).Length / 1KB
    Write-Host "[SUCCESS] Standalone Installer compiled successfully!" -ForegroundColor Green
    Write-Host "Output: $setupExe ($([math]::Round($size, 1)) KB)" -ForegroundColor Green
} else {
    Write-Error "Setup compilation failed with exit code $($process.ExitCode)"
    exit $process.ExitCode
}
