# OmniRoute Console - Local Build Script
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$srcDir = Join-Path $root "src"
$assetsDir = Join-Path $root "assets"
$binDir = Join-Path $root "bin"
$ico = Join-Path $assetsDir "omniroute.ico"
$outExe = Join-Path $binDir "OmniRouteConsole.exe"

if (-not (Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir -Force | Out-Null
}

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path $csc)) {
    Write-Error "csc.exe not found on this system."
    exit 1
}

Write-Host "Compiling OmniRoute Console..." -ForegroundColor Cyan

$sources = @(
    (Join-Path $srcDir "Program.cs"),
    (Join-Path $srcDir "UITheme.cs"),
    (Join-Path $srcDir "ServerManager.cs"),
    (Join-Path $srcDir "LauncherForm.cs")
)

foreach ($src in $sources) {
    if (-not (Test-Path $src)) {
        Write-Error "Missing source file: $src"
        exit 1
    }
}

$compileArgs = @(
    "/target:winexe",
    "/optimize+",
    "/platform:x64",
    "/win32icon:`"$ico`"",
    "/out:`"$outExe`""
) + ($sources | ForEach-Object { "`"$_`"" })

Write-Host "Invoking $csc with embedded icon..." -ForegroundColor Gray
$process = Start-Process -FilePath $csc -ArgumentList ($compileArgs -join " ") -NoNewWindow -Wait -PassThru

if ($process.ExitCode -eq 0 -and (Test-Path $outExe)) {
    Write-Host "[SUCCESS] OmniRoute Console compiled successfully at: $outExe" -ForegroundColor Green
} else {
    Write-Error "Build failed with exit code $($process.ExitCode)"
    exit $process.ExitCode
}
