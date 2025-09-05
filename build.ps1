#!/usr/bin/env pwsh
# Nova Chess Build Script
# Creates a single-file, self-contained Windows executable

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "publish",
    [switch]$Clean = $false
)

Write-Host "🏗️  Nova Chess Build Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Clean previous build if requested
if ($Clean) {
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path $OutputDir) {
        Remove-Item $OutputDir -Recurse -Force
    }
    dotnet clean --configuration $Configuration
}

# Restore dependencies
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Green
dotnet restore

# Build the solution
Write-Host "🔨 Building solution..." -ForegroundColor Green
dotnet build --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Publish single-file executable
Write-Host "📦 Publishing single-file executable..." -ForegroundColor Green
dotnet publish src/NovaChess.App/NovaChess.App.csproj `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $OutputDir `
    --property:PublishSingleFile=true `
    --property:IncludeNativeLibrariesForSelfExtract=true `
    --property:PublishTrimmed=false `
    --no-restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
    Write-Host "📁 Output location: $OutputDir" -ForegroundColor Cyan
    Write-Host "🎮 Executable: $OutputDir/NovaChess.App.exe" -ForegroundColor Cyan
    
    $exePath = Join-Path $OutputDir "NovaChess.App.exe"
    if (Test-Path $exePath) {
        $fileInfo = Get-Item $exePath
        $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        Write-Host "📏 File size: $fileSizeMB MB" -ForegroundColor Cyan
    }
} else {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit 1
}

