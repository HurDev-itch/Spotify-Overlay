# Build Orchestrator for Spotify Overlay
$ErrorActionPreference = "Stop"

Write-Host "Starting Spotify Overlay Build Process..." -ForegroundColor Cyan

# 1. Build C++ Hook DLL
Write-Host "`n[1/3] Building C++ DirectX Hook DLL..." -ForegroundColor Yellow
$hookDir = "src\SpotifyOverlay.Hook"
if (-Not (Test-Path "$hookDir\build")) {
    New-Item -ItemType Directory -Force -Path "$hookDir\build" | Out-Null
}
Push-Location "$hookDir\build"
cmake .. -A x64
cmake --build . --config Release
Pop-Location

# 1.5 Build C++ TestHost
Write-Host "`n[1.5/3] Building C++ TestHost Sandbox..." -ForegroundColor Yellow
$testHostDir = "src\SpotifyOverlay.TestHost"
if (-Not (Test-Path "$testHostDir\build")) {
    New-Item -ItemType Directory -Force -Path "$testHostDir\build" | Out-Null
}
Push-Location "$testHostDir\build"
cmake .. -A x64
cmake --build . --config Release
Pop-Location

# 2. Build C# Solution
Write-Host "`n[2/3] Publishing C# .NET 8 WinUI Application..." -ForegroundColor Yellow
$appDir = "src\SpotifyOverlay.App"
dotnet publish "$appDir\SpotifyOverlay.App.csproj" -c Release -p:Platform=x64 -r win-x64 --self-contained true

# 3. Compile Inno Setup Installer
Write-Host "`n[3/3] Compiling Inno Setup Installer..." -ForegroundColor Yellow
# Assuming ISCC is in the system PATH
$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (Test-Path $isccPath) {
    & $isccPath "SpotifyOverlay.iss"
    Write-Host "`nBuild Complete! Installer is located in the Output folder." -ForegroundColor Green
} else {
    Write-Host "`nInno Setup (ISCC.exe) not found. Skipping installer generation." -ForegroundColor Red
    Write-Host "Binaries are ready in their respective publish/build directories." -ForegroundColor Green
}
