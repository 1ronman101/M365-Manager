# Build and Package M365 Manager
# Run this script from the project root folder

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  M365 Manager Build & Package Script  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path ".\publish") { Remove-Item -Recurse -Force ".\publish" }
if (Test-Path ".\bin") { Remove-Item -Recurse -Force ".\bin" }
if (Test-Path ".\obj") { Remove-Item -Recurse -Force ".\obj" }

# Restore packages
Write-Host "[2/4] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed!" -ForegroundColor Red
    exit 1
}

# Build the app (using bin output, not Publish - WinUI 3 publish has issues with XBF files)
Write-Host "[3/4] Building self-contained application..." -ForegroundColor Yellow

$binPath = ".\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64"

# Find and use MSBuild from Visual Studio (required for Windows App SDK)
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2>$null | Select-Object -First 1

if ($msbuild -and (Test-Path $msbuild)) {
    Write-Host "Using MSBuild: $msbuild" -ForegroundColor Gray
    & $msbuild M365Manager.csproj /t:Rebuild /p:Configuration=$Configuration /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:SelfContained=true /v:minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "ERROR: Visual Studio with MSBuild is required to build Windows App SDK self-contained apps." -ForegroundColor Red
    Write-Host "Please install Visual Studio 2022 or later with the '.NET Desktop Development' workload." -ForegroundColor Yellow
    exit 1
}

# Verify the build output
if (-not (Test-Path "$binPath\M365Manager.exe")) {
    Write-Host "ERROR: M365Manager.exe not found in bin folder!" -ForegroundColor Red
    exit 1
}

$fileCount = (Get-ChildItem $binPath -Recurse -File).Count
Write-Host "Built $fileCount files to $binPath" -ForegroundColor Gray

Write-Host "[4/4] Build complete!" -ForegroundColor Green
Write-Host ""

# Check if Inno Setup is installed
$InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $InnoSetupPath)) {
    $InnoSetupPath = "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
}

if (Test-Path $InnoSetupPath) {
    Write-Host "Building installer with Inno Setup..." -ForegroundColor Yellow
    
    # Create installer output directory
    if (-not (Test-Path ".\publish\installer")) {
        New-Item -ItemType Directory -Path ".\publish\installer" | Out-Null
    }
    
    # Run Inno Setup compiler
    & $InnoSetupPath ".\Installer\M365ManagerSetup.iss"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  Installer created successfully!       " -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Installer location: .\publish\installer\M365Manager_Setup_$Version.exe" -ForegroundColor Cyan
    } else {
        Write-Host "Installer creation failed!" -ForegroundColor Red
    }
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "  Inno Setup not found!                " -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To create the installer:" -ForegroundColor White
    Write-Host "1. Download Inno Setup from: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
    Write-Host "2. Install it (default location)" -ForegroundColor Gray
    Write-Host "3. Run this script again, OR" -ForegroundColor Gray
    Write-Host "4. Open Installer\M365ManagerSetup.iss in Inno Setup and click Build" -ForegroundColor Gray
    Write-Host ""
    Write-Host "The published files are ready in: .\publish\win-x64" -ForegroundColor Cyan
}
