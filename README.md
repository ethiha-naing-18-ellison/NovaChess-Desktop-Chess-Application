# Nova ✦ Chess – Desktop

A production-ready Windows desktop Chess application built with C# WPF and .NET 8, featuring a modern dark theme, powerful AI integration, and comprehensive chess analysis tools.

![Nova Chess](assets/icons/novachess.png)

## ✨ Features

### 🎮 Game Modes
- **Local PvP**: Two players at one PC
- **Player vs AI**: Using Stockfish engine with adjustable skill levels (0-20)
- **Analysis Mode**: Free move mode with engine evaluation

### ♟️ Chess Rules & Mechanics
- Complete legal move generation for all pieces
- Check, checkmate, and stalemate detection
- Special moves: Castling, En Passant, Pawn Promotion
- Draw detection: Threefold repetition, 50-move rule, insufficient material
- SAN notation generation with disambiguation

### 🎨 User Experience
- **Dark Theme**: Polished dark gradient background (#0f1221 → #171a2b)
- **Smooth Animations**: 200-300ms piece move animations
- **Drag & Drop**: Intuitive piece movement
- **Legal Move Highlighting**: Visual feedback for valid moves
- **Move List**: SAN notation with current ply highlighting
- **Clocks/Timers**: Presets (3+2, 5+5, 10+0) with pause/resume

### 🔧 Analysis & AI
- **Engine Integration**: UCI protocol support for Stockfish
- **Evaluation Bar**: Real-time engine score display
- **Principal Variation**: Best move sequences
- **Adjustable Depth**: 1-20 ply analysis
- **Skill Levels**: 0-20 for AI opponents

### 📁 File Support
- **PGN Import/Export**: Full game notation support
- **FEN Load/Save**: Position loading and saving
- **Clipboard Integration**: Copy FEN to clipboard
- **Game Library**: Local PGN database management

### ⚙️ Customization
- **Board Themes**: Wood, Slate, Mono
- **Piece Sets**: Classic, Minimalist
- **Sound Effects**: Move, capture, check sounds with volume control
- **Coordinates**: Toggle board coordinates
- **Highlights**: Customizable move and check highlighting

## 🏗️ Architecture

### Solution Structure
```
NovaChess/
├── src/
│   ├── NovaChess.App/          # WPF Application (Views, ViewModels)
│   ├── NovaChess.Core/         # Chess Logic Engine
│   ├── NovaChess.Infrastructure/ # Services & External Dependencies
│   └── NovaChess.Tests/        # Unit Tests
├── assets/                     # Icons, Pieces, Sounds
├── build/                      # Build Scripts
└── README.md
```

### Technology Stack
- **.NET 8**: Latest LTS framework
- **WPF**: Windows Presentation Foundation
- **MVVM**: CommunityToolkit.Mvvm for clean architecture
- **Serilog**: Structured logging with file sinks
- **System.Text.Json**: High-performance JSON handling
- **xUnit**: Unit testing framework

## 🚀 Getting Started

### Prerequisites
- Windows 10/11 (x64)
- .NET 8 SDK
- Visual Studio 2022 or VS Code

### Build & Run

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/NovaChess-Desktop.git
   cd NovaChess-Desktop
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Run the application**
   ```bash
   dotnet run --project src/NovaChess.App
   ```

### Build Single-File Executable

Use the provided PowerShell script to create a self-contained executable:

```powershell
.\build\publish-win-x64.ps1
```

This will create `./publish/win-x64/NovaChess.App.exe` - a single-file executable that runs on any Windows 10/11 x64 system without requiring .NET installation.

## 🎯 Usage

### Starting a New Game
1. Click "♟️ New Game" from the home screen
2. Choose game mode (PvP, vs AI, Analysis)
3. Select time control (3+2, 5+5, 10+0, or custom)
4. Configure AI settings (skill level, depth, move time)
5. Click "Start Game"

### Playing Chess
- **Move Pieces**: Click and drag, or click source then destination
- **Legal Moves**: Highlighted automatically
- **Special Moves**: Castling, en passant, and promotion handled automatically
- **Game State**: Check, checkmate, and draw detection
- **Undo/Redo**: Full move history with Ctrl+Z/Ctrl+Y

### Analysis Mode
1. Click "🔍 Analysis" from the home screen
2. Set up any position by dragging pieces
3. Toggle engine analysis on/off
4. Adjust analysis depth and time
5. View evaluation bar and principal variation

### Keyboard Shortcuts
- `Ctrl+Z`: Undo move
- `Ctrl+Y`: Redo move
- `Ctrl+S`: Save PGN
- `Ctrl+O`: Open PGN
- `Space`: Pause/Resume clocks
- `Ctrl+C`: Copy FEN to clipboard

## 🔧 Configuration

### Settings Location
- **Settings**: `%AppData%\NovaChess\config.json`
- **Games**: `%AppData%\NovaChess\games\`
- **Logs**: `%AppData%\NovaChess\logs\`
- **Engine**: `%AppData%\NovaChess\engine\stockfish.exe`

### Engine Configuration
- **Default Path**: Automatically detects bundled Stockfish
- **Custom Path**: Override in settings
- **Skill Level**: 0-20 (0 = random, 20 = strongest)
- **Analysis Depth**: 1-50 ply
- **Move Time**: Milliseconds per move

## 🧪 Testing

The project includes comprehensive unit tests for all chess logic:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test src/NovaChess.Tests/
```

### Test Coverage
- **Move Generation**: All piece types and special moves
- **Game Rules**: Check, checkmate, stalemate detection
- **Draw Rules**: Repetition, 50-move, insufficient material
- **PGN/FEN**: Import/export round-trip testing
- **Clock Logic**: Timer and increment functionality

## 📦 Distribution

### Single-File Executable
The build script creates a self-contained executable that includes:
- All .NET runtime components
- Embedded dependencies
- No external .NET installation required
- Windows 10/11 x64 compatibility

### File Size
- **Release Build**: ~50-80 MB
- **Optimized**: Trimming and ReadyToRun compilation
- **Self-Contained**: Includes all necessary runtime components

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow C# coding conventions
- Write unit tests for new features
- Use MVVM pattern for UI logic
- Keep Core project UI-agnostic
- Update documentation for new features

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**Note**: Stockfish engine is licensed under GPLv3. The Stockfish binary is bundled for convenience but can be replaced with any UCI-compatible chess engine.

## 🙏 Acknowledgments

- **Stockfish Team**: For the excellent chess engine
- **Microsoft**: .NET 8 and WPF frameworks
- **Community**: MVVM toolkit and open-source libraries

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/NovaChess-Desktop/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/NovaChess-Desktop/discussions)
- **Wiki**: [Project Wiki](https://github.com/yourusername/NovaChess-Desktop/wiki)

---

**Nova Chess – Desktop** - Where elegance meets intelligence in the game of kings.
