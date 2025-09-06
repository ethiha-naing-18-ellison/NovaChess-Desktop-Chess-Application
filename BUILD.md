# 🏗️ NovaChess Build Guide

Complete guide for building NovaChess - Tech-Noble executable from source code.

## 📋 Prerequisites

### **Required Software**
- **.NET 8.0 SDK** or later
  - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify installation: `dotnet --version` (should show 8.x.x)
- **Visual Studio 2022** (recommended) or **Visual Studio Code**
- **Windows 10/11** (for Windows-specific builds)
- **PowerShell 5.1+** (for build scripts)

### **System Requirements**
- **OS**: Windows 10 version 1903 or later
- **RAM**: 4GB minimum, 8GB recommended
- **Storage**: 2GB free space for build artifacts
- **CPU**: x64 architecture

## 🗂️ Project Structure

```
NovaChess-Desktop-Chess-Application-1/
├── src/
│   ├── NovaChess.App/           # Main WPF Application
│   │   ├── assets/              # App icons and logos
│   │   ├── Controls/            # Custom WPF controls
│   │   ├── Models/              # Data models
│   │   ├── ViewModels/          # MVVM view models
│   │   ├── Views/               # XAML views
│   │   └── NovaChess.App.csproj # Main project file
│   ├── NovaChess.Core/          # Chess engine core
│   │   ├── *.cs                 # Chess logic files
│   │   └── NovaChess.Core.csproj
│   ├── NovaChess.Infrastructure/ # Services and interfaces
│   │   ├── Interfaces/          # Service contracts
│   │   ├── Services/            # Service implementations
│   │   └── NovaChess.Infrastructure.csproj
│   └── NovaChess.Tests/         # Unit tests
│       ├── *.cs                 # Test files
│       └── NovaChess.Tests.csproj
├── build/                       # Build scripts
├── assets/                      # Global assets
├── NovaChess.sln               # Solution file
├── build.ps1                   # Main build script
└── BUILD.md                    # This file
```

## 🚀 Build Methods

### **Method 1: Quick Build (Recommended)**

Use the provided PowerShell build script:

```powershell
# Navigate to project root
cd "C:\xampp\htdocs\NovaChess-Desktop-Chess-Application-1"

# Run the build script
.\build.ps1

# Or with custom parameters
.\build.ps1 -Configuration Release -OutputDir "MyBuild" -Clean
```

**Output**: `publish/NovaChess.App.exe` (single-file executable)

### **Method 2: Manual Build Commands**

```powershell
# 1. Restore NuGet packages
dotnet restore

# 2. Build the solution
dotnet build --configuration Release

# 3. Publish single-file executable
dotnet publish src/NovaChess.App/NovaChess.App.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output NovaChess-Distribution `
    --property:PublishSingleFile=true `
    --property:IncludeNativeLibrariesForSelfExtract=true
```

### **Method 3: Advanced Build Script**

Use the advanced build script in the `build/` folder:

```powershell
.\build\publish-win-x64.ps1 -Configuration Release
```

## ⚙️ Build Configurations

### **Debug Build**
- **Purpose**: Development and debugging
- **Size**: Larger (~200MB)
- **Performance**: Slower startup
- **Symbols**: Includes debug symbols
- **Command**: `dotnet build --configuration Debug`

### **Release Build**
- **Purpose**: Production distribution
- **Size**: Optimized (~160MB)
- **Performance**: Faster startup
- **Symbols**: Stripped debug symbols
- **Command**: `dotnet build --configuration Release`

## 📦 Build Outputs

### **Single-File Executable (Recommended)**
- **File**: `NovaChess.App.exe`
- **Size**: ~160MB
- **Dependencies**: Self-contained (no .NET runtime required)
- **Distribution**: Copy single file anywhere

### **Framework-Dependent**
- **Folder**: Multiple files (EXE + DLLs)
- **Size**: ~50MB
- **Dependencies**: Requires .NET 8.0 runtime on target machine
- **Distribution**: Copy entire folder

### **Self-Contained**
- **Folder**: All dependencies included
- **Size**: ~200MB
- **Dependencies**: None (fully self-contained)
- **Distribution**: Copy entire folder

## 🎯 Build Parameters

### **Runtime Identifiers**
- `win-x64`: Windows 64-bit (recommended)
- `win-x86`: Windows 32-bit
- `win-arm64`: Windows ARM64

### **Publish Properties**
- `PublishSingleFile=true`: Creates single executable
- `PublishTrimmed=true`: Reduces file size (may break reflection)
- `PublishReadyToRun=true`: Improves startup performance
- `IncludeNativeLibrariesForSelfExtract=true`: Includes native libraries

## 🔧 Troubleshooting

### **Common Build Errors**

#### **Error: "dotnet command not found"**
```bash
# Solution: Install .NET 8.0 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
```

#### **Error: "Package restore failed"**
```bash
# Solution: Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore
```

#### **Error: "Build failed with warnings"**
```bash
# Solution: Check for nullable reference warnings
# Most warnings are safe to ignore for now
```

#### **Error: "Icon file not found"**
```bash
# Solution: Ensure app-icon.ico exists in src/NovaChess.App/assets/
# If missing, convert app-icon.png to ICO format
```

### **Build Performance Issues**

#### **Slow Build Times**
- Use `--no-restore` flag for subsequent builds
- Exclude test projects: `dotnet build --project src/NovaChess.App/`
- Use `PublishTrimmed=true` for smaller output

#### **Large File Sizes**
- Use `PublishTrimmed=true` (test thoroughly)
- Remove unused assets from `assets/` folder
- Consider framework-dependent deployment

## 📁 Asset Requirements

### **Required Assets**
```
src/NovaChess.App/assets/
├── app-icon.ico        # Application icon (32x32, 48x48, 256x256)
├── app-icon.png        # Source icon file
└── app-logo.png        # Application logo
```

### **Asset Specifications**
- **app-icon.ico**: Multi-resolution ICO file (16x16, 32x32, 48x48, 256x256)
- **app-logo.png**: PNG format, recommended 512x512 or higher
- **File sizes**: Keep under 1MB each for optimal performance

## 🧪 Testing the Build

### **Pre-Build Testing**
```powershell
# Run unit tests
dotnet test

# Run specific test project
dotnet test src/NovaChess.Tests/
```

### **Post-Build Testing**
```powershell
# Test the executable
.\NovaChess-Distribution\NovaChess.App.exe

# Check file properties
Get-ItemProperty .\NovaChess-Distribution\NovaChess.App.exe | Select-Object Name, Length, VersionInfo
```

## 📊 Build Statistics

### **Typical Build Times**
- **Clean build**: 30-60 seconds
- **Incremental build**: 5-15 seconds
- **Publish**: 10-30 seconds

### **Output Sizes**
- **Debug build**: ~200MB
- **Release build**: ~160MB
- **Trimmed build**: ~120MB (may break functionality)

## 🚀 Distribution

### **Single-File Distribution**
1. Build with `PublishSingleFile=true`
2. Copy `NovaChess.App.exe` to target location
3. No additional files needed

### **Folder Distribution**
1. Build with `--self-contained true`
2. Copy entire output folder
3. Run `NovaChess.App.exe` from folder

### **Installer Creation**
Consider using tools like:
- **WiX Toolset**: Professional Windows installer
- **Inno Setup**: Free installer creator
- **NSIS**: Open-source installer system

## 🔄 Continuous Integration

### **GitHub Actions Example**
```yaml
name: Build NovaChess
on: [push, pull_request]
jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - name: Build
      run: .\build.ps1 -Configuration Release
    - name: Upload Artifacts
      uses: actions/upload-artifact@v3
      with:
        name: NovaChess-Executable
        path: publish/NovaChess.App.exe
```

## 📝 Build Scripts Reference

### **Main Build Script (build.ps1)**
```powershell
# Parameters:
# -Configuration: Debug|Release (default: Release)
# -OutputDir: Output directory (default: publish)
# -Clean: Clean previous builds (default: false)

# Usage examples:
.\build.ps1
.\build.ps1 -Configuration Debug
.\build.ps1 -OutputDir "MyBuild" -Clean
```

### **Advanced Build Script (build/publish-win-x64.ps1)**
```powershell
# Parameters:
# -Configuration: Debug|Release (default: Release)
# -Framework: Target framework (default: net8.0)
# -Runtime: Runtime identifier (default: win-x64)

# Usage examples:
.\build\publish-win-x64.ps1
.\build\publish-win-x64.ps1 -Configuration Debug
```

## 🎯 Quick Start Commands

```powershell
# 1. Clone and navigate to project
cd "C:\xampp\htdocs\NovaChess-Desktop-Chess-Application-1"

# 2. Quick build
.\build.ps1

# 3. Run the application
.\NovaChess-Distribution\NovaChess.App.exe
```

## 📞 Support

If you encounter build issues:
1. Check this BUILD.md file
2. Verify all prerequisites are installed
3. Try a clean build: `.\build.ps1 -Clean`
4. Check the GitHub issues page
5. Create a new issue with build logs

---

**Last Updated**: December 2024  
**Build Version**: 1.0.0  
**Target Framework**: .NET 8.0  
**Platform**: Windows x64
