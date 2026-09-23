# PowerShell build script for FliqloClock

$csc64 = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$csc32 = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
$csc = ""

if (Test-Path $csc64) {
    $csc = $csc64
    Write-Host "Found 64-bit C# Compiler at: $csc"
} elseif (Test-Path $csc32) {
    $csc = $csc32
    Write-Host "Found 32-bit C# Compiler at: $csc"
} else {
    Write-Error "Could not find csc.exe compiler in .NET Framework v4.0.30319 paths."
    exit 1
}

# Change directory to the script's directory to ensure relative paths are resolved properly
$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
if (-not $PSScriptRoot) {
    $PSScriptRoot = Get-Location
}
Set-Location $PSScriptRoot

Write-Host "Compiling files: Program.cs, Settings.cs, SettingsForm.cs, FlipCard.cs, ScreensaverForm.cs..."

# Compile options:
# /target:winexe -> creates a GUI executable instead of console app (so no console flashes)
# /out:FliqloClock.scr -> rename target to screensaver executable (.scr)
# /optimize -> optimize the executable
$argsList = @(
    "/target:winexe",
    "/out:FliqloClock.scr",
    "/optimize",
    "/r:System.dll,System.Drawing.dll,System.Windows.Forms.dll",
    "Program.cs",
    "Settings.cs",
    "SettingsForm.cs",
    "FlipCard.cs",
    "ScreensaverForm.cs"
)

& $csc $argsList

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build Succeeded! Output: C:\Users\LuC\FliqloClock\FliqloClock.scr"
} else {
    Write-Error "Build Failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}
