# Nova Chess - Desktop Build Script
# Builds a single-file executable for Windows x64

param(
    [string]$Configuration = "Release",
    [string]$Framework = "net8.0",
    [string]$Runtime = "win-x64"
)

Write-Host "🎯 Building Nova Chess - Desktop..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Framework: $Framework" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Clean previous builds
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Blue
    if (Test-Path "./publish") {
        Remove-Item "./publish" -Recurse -Force
    }
    
    # Restore packages
    Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Blue
    dotnet restore
    
    # Build solution
    Write-Host "🔨 Building solution..." -ForegroundColor Blue
    dotnet build -c $Configuration --no-restore
    
    # Run tests
    Write-Host "🧪 Running tests..." -ForegroundColor Blue
    dotnet test --no-build --verbosity normal
    
    # Publish application
    Write-Host "📤 Publishing application..." -ForegroundColor Blue
    dotnet publish ./src/NovaChess.App/NovaChess.App.csproj `
        -c $Configuration `
        -r $Runtime `
        -f $Framework `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=true `
        -p:PublishReadyToRun=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        --self-contained true `
        -o ./publish/$Runtime `
        --verbosity normal
    
    # Verify output
    $exePath = "./publish/$Runtime/NovaChess.App.exe"
    if (Test-Path $exePath) {
        $fileSize = (Get-Item $exePath).Length / 1MB
        Write-Host "✅ Build completed successfully!" -ForegroundColor Green
        Write-Host "📁 Output: $exePath" -ForegroundColor Green
        Write-Host "📏 File size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Green
        
        # List all published files
        Write-Host "📋 Published files:" -ForegroundColor Blue
        Get-ChildItem "./publish/$Runtime" | ForEach-Object {
            Write-Host "   $($_.Name)" -ForegroundColor Gray
        }
    } else {
        throw "Executable not found at expected location: $exePath"
    }
    
} catch {
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "🎉 Build script completed!" -ForegroundColor Green
