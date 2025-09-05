using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaChess.Core;
using NovaChess.Infrastructure.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace NovaChess.App.ViewModels;

public partial class PgnLibraryViewModel : ObservableObject
{
    private readonly ILogService _logService;
    private readonly Arbiter _arbiter;
    
    [ObservableProperty]
    private PgnGameInfo? _selectedGame;
    
    [ObservableProperty]
    private string _searchText = "";
    
    [ObservableProperty]
    private string _selectedPgnContent = "";
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private string _libraryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NovaChess", "Games");
    
    [ObservableProperty]
    private string _statusMessage = "Ready";
    
    public ObservableCollection<PgnGameInfo> Games { get; } = new();
    public ObservableCollection<PgnGameInfo> FilteredGames { get; } = new();
    
    public IRelayCommand ImportPgnCommand { get; }
    public IRelayCommand ExportPgnCommand { get; }
    public IRelayCommand DeleteGameCommand { get; }
    public IRelayCommand LoadGameCommand { get; }
    public IRelayCommand RefreshLibraryCommand { get; }
    public IRelayCommand SearchGamesCommand { get; }
    public IRelayCommand ClearSearchCommand { get; }
    public IRelayCommand OpenLibraryFolderCommand { get; }
    public IRelayCommand CreateSampleGamesCommand { get; }
    
    public PgnLibraryViewModel(ILogService logService)
    {
        _logService = logService;
        _arbiter = new Arbiter(new ChessMoveGenerator());
        
        ImportPgnCommand = new AsyncRelayCommand(ImportPgnFile);
        ExportPgnCommand = new AsyncRelayCommand(ExportSelectedGame);
        DeleteGameCommand = new AsyncRelayCommand(DeleteSelectedGame);
        LoadGameCommand = new RelayCommand(LoadSelectedGame);
        RefreshLibraryCommand = new AsyncRelayCommand(RefreshLibrary);
        SearchGamesCommand = new RelayCommand(SearchGames);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        OpenLibraryFolderCommand = new RelayCommand(OpenLibraryFolder);
        CreateSampleGamesCommand = new AsyncRelayCommand(CreateSampleGames);
        
        // Initialize library directory
        EnsureLibraryDirectory();
        
        // Load games on startup
        _ = RefreshLibrary();
    }
    
    private void EnsureLibraryDirectory()
    {
        try
        {
            if (!Directory.Exists(LibraryPath))
            {
                Directory.CreateDirectory(LibraryPath);
                _logService.Information($"Created PGN library directory: {LibraryPath}");
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error creating library directory: {ex.Message}");
        }
    }
    
    private async Task ImportPgnFile()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import PGN File",
                Filter = "PGN Files (*.pgn)|*.pgn|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Multiselect = true
            };
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusMessage = "Importing PGN files...";
                
                int importedCount = 0;
                
                foreach (var fileName in dialog.FileNames)
                {
                    var imported = await ImportSinglePgnFile(fileName);
                    importedCount += imported;
                }
                
                StatusMessage = $"Successfully imported {importedCount} games";
                await RefreshLibrary();
                
                MessageBox.Show($"Successfully imported {importedCount} games!", "Import Complete", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
            MessageBox.Show($"Error importing PGN: {ex.Message}", "Import Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
            _logService.Error($"PGN import error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task<int> ImportSinglePgnFile(string fileName)
    {
        var content = await File.ReadAllTextAsync(fileName);
        var games = ParseMultiplePgnGames(content);
        int importedCount = 0;
        
        foreach (var game in games)
        {
            var gameInfo = CreateGameInfo(game);
            var targetPath = Path.Combine(LibraryPath, $"{gameInfo.Id}.pgn");
            
            await File.WriteAllTextAsync(targetPath, PgnParser.ToPgn(game));
            importedCount++;
        }
        
        return importedCount;
    }
    
    private List<PgnGame> ParseMultiplePgnGames(string content)
    {
        var games = new List<PgnGame>();
        var gameStrings = content.Split(new[] { "\n\n\n", "\r\n\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var gameString in gameStrings)
        {
            if (string.IsNullOrWhiteSpace(gameString)) continue;
            
            try
            {
                var game = PgnParser.Parse(gameString.Trim());
                if (game.Moves.Count > 0) // Only add games with moves
                {
                    games.Add(game);
                }
            }
            catch (Exception ex)
            {
                _logService.Warning($"Failed to parse game: {ex.Message}");
            }
        }
        
        return games;
    }
    
    private PgnGameInfo CreateGameInfo(PgnGame game)
    {
        return new PgnGameInfo
        {
            Id = Guid.NewGuid().ToString(),
            White = game.Tags.GetValueOrDefault("White", "Unknown"),
            Black = game.Tags.GetValueOrDefault("Black", "Unknown"),
            Event = game.Tags.GetValueOrDefault("Event", "Casual Game"),
            Site = game.Tags.GetValueOrDefault("Site", "Unknown"),
            Date = game.Tags.GetValueOrDefault("Date", DateTime.Now.ToString("yyyy.MM.dd")),
            Round = game.Tags.GetValueOrDefault("Round", "1"),
            Result = game.Result,
            MoveCount = game.Moves.Count,
            Opening = DetermineOpening(game),
            ImportDate = DateTime.Now
        };
    }
    
    private string DetermineOpening(PgnGame game)
    {
        // Simple opening detection based on first few moves
        if (game.Moves.Count < 2) return "Unknown";
        
        var firstMove = game.Moves[0];
        if (firstMove.Piece == PieceType.Pawn)
        {
            var dest = firstMove.Destination.ToAlgebraic().ToLower();
            return dest switch
            {
                "e4" => "King's Pawn",
                "d4" => "Queen's Pawn",
                "c4" => "English Opening",
                "nf3" => "Réti Opening",
                _ => "Other"
            };
        }
        
        return "Other";
    }
    
    private async Task ExportSelectedGame()
    {
        if (SelectedGame == null)
        {
            MessageBox.Show("Please select a game to export.", "No Game Selected", 
                          MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export PGN Game",
                Filter = "PGN Files (*.pgn)|*.pgn|Text Files (*.txt)|*.txt",
                FileName = $"{SelectedGame.White}_vs_{SelectedGame.Black}_{SelectedGame.Date.Replace(".", "-")}.pgn"
            };
            
            if (dialog.ShowDialog() == true)
            {
                var gamePath = Path.Combine(LibraryPath, $"{SelectedGame.Id}.pgn");
                if (File.Exists(gamePath))
                {
                    var content = await File.ReadAllTextAsync(gamePath);
                    await File.WriteAllTextAsync(dialog.FileName, content);
                    
                    StatusMessage = $"Game exported to {dialog.FileName}";
                    MessageBox.Show("Game exported successfully!", "Export Complete", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting game: {ex.Message}", "Export Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
            _logService.Error($"PGN export error: {ex.Message}");
        }
    }
    
    private async Task DeleteSelectedGame()
    {
        if (SelectedGame == null)
        {
            MessageBox.Show("Please select a game to delete.", "No Game Selected", 
                          MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var result = MessageBox.Show($"Are you sure you want to delete the game '{SelectedGame.White} vs {SelectedGame.Black}'?", 
                                   "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var gamePath = Path.Combine(LibraryPath, $"{SelectedGame.Id}.pgn");
                if (File.Exists(gamePath))
                {
                    File.Delete(gamePath);
                }
                
                Games.Remove(SelectedGame);
                FilteredGames.Remove(SelectedGame);
                SelectedGame = null;
                SelectedPgnContent = "";
                
                StatusMessage = "Game deleted successfully";
                MessageBox.Show("Game deleted successfully!", "Delete Complete", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting game: {ex.Message}", "Delete Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                _logService.Error($"PGN delete error: {ex.Message}");
            }
        }
    }
    
    private void LoadSelectedGame()
    {
        if (SelectedGame == null) return;
        
        // TODO: Integrate with GameViewModel to load the game
        MessageBox.Show("Game loading feature will be integrated with the main game view.", "Feature Coming Soon", 
                      MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private async Task RefreshLibrary()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading games...";
            
            Games.Clear();
            FilteredGames.Clear();
            
            if (!Directory.Exists(LibraryPath))
            {
                StatusMessage = "No games found. Import PGN files to get started.";
                return;
            }
            
            var pgnFiles = Directory.GetFiles(LibraryPath, "*.pgn");
            
            foreach (var file in pgnFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var game = PgnParser.Parse(content);
                    var gameInfo = CreateGameInfo(game);
                    gameInfo.Id = Path.GetFileNameWithoutExtension(file);
                    
                    Games.Add(gameInfo);
                }
                catch (Exception ex)
                {
                    _logService.Warning($"Failed to load game {file}: {ex.Message}");
                }
            }
            
            // Copy to filtered collection
            foreach (var game in Games)
            {
                FilteredGames.Add(game);
            }
            
            StatusMessage = $"Loaded {Games.Count} games";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading games: {ex.Message}";
            _logService.Error($"Library refresh error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void SearchGames()
    {
        FilteredGames.Clear();
        
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var game in Games)
            {
                FilteredGames.Add(game);
            }
        }
        else
        {
            var searchLower = SearchText.ToLower();
            foreach (var game in Games)
            {
                if (game.White.ToLower().Contains(searchLower) ||
                    game.Black.ToLower().Contains(searchLower) ||
                    game.Event.ToLower().Contains(searchLower) ||
                    game.Opening.ToLower().Contains(searchLower))
                {
                    FilteredGames.Add(game);
                }
            }
        }
        
        StatusMessage = $"Found {FilteredGames.Count} matching games";
    }
    
    private void ClearSearch()
    {
        SearchText = "";
        SearchGames();
    }
    
    private void OpenLibraryFolder()
    {
        try
        {
            EnsureLibraryDirectory();
            System.Diagnostics.Process.Start("explorer.exe", LibraryPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening library folder: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task CreateSampleGames()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating sample games...";
            
            var sampleGames = GetSamplePgnGames();
            int createdCount = 0;
            
            foreach (var (name, content) in sampleGames)
            {
                var filePath = Path.Combine(LibraryPath, $"Sample_{name}.pgn");
                if (!File.Exists(filePath))
                {
                    await File.WriteAllTextAsync(filePath, content);
                    createdCount++;
                }
            }
            
            await RefreshLibrary();
            StatusMessage = $"Created {createdCount} sample games";
            
            if (createdCount > 0)
            {
                MessageBox.Show($"Created {createdCount} sample games!", "Sample Games Created", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Sample games already exist.", "Info", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating samples: {ex.Message}";
            MessageBox.Show($"Error creating sample games: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private List<(string name, string content)> GetSamplePgnGames()
    {
        return new List<(string, string)>
        {
            ("Immortal_Game", @"[Event ""Immortal Game""]
[Site ""London""]
[Date ""1851.06.21""]
[Round ""?""]
[White ""Adolf Anderssen""]
[Black ""Lionel Kieseritzky""]
[Result ""1-0""]

1.e4 e5 2.f4 exf4 3.Bc4 Qh4+ 4.Kf1 b5 5.Bxb5 Nf6 6.Nf3 Qh6 7.d3 Nh5 8.Nh4 Qg5 9.Nf5 c6 10.g4 Nf6 11.Rg1 cxb5 12.h4 Qg6 13.h5 Qg5 14.Qf3 Ng8 15.Bxf4 Qf6 16.Nc3 Bc5 17.Nd5 Qxb2 18.Bd6 Qxa1+ 19.Ke2 Bxg1 20.e5 Na6 21.Nxg7+ Kd8 22.Qf6+ Nxf6 23.Be7# 1-0"),
            
            ("Evergreen_Game", @"[Event ""Evergreen Game""]
[Site ""Berlin""]
[Date ""1852.??.??""]
[Round ""?""]
[White ""Adolf Anderssen""]
[Black ""Jean Dufresne""]
[Result ""1-0""]

1.e4 e5 2.Nf3 Nc6 3.Bc4 Bc5 4.b4 Bxb4 5.c3 Ba5 6.d4 exd4 7.O-O d3 8.Qb3 Qf6 9.e5 Qg6 10.Re1 Nge7 11.Ba3 b5 12.Qxb5 Rb8 13.Qa4 Bb6 14.Nbd2 Bb7 15.Ne4 Qf5 16.Bxd3 Qh5 17.Nf6+ gxf6 18.exf6 Rg8 19.Rad1 Qxf3 20.Rxe7+ Nxe7 21.Qxd7+ Kxd7 22.Bf5+ Ke8 23.Bd7+ Kf8 24.Bxe7# 1-0"),
            
            ("Opera_Game", @"[Event ""Opera Game""]
[Site ""Paris Opera""]
[Date ""1858.??.??""]
[Round ""?""]
[White ""Paul Morphy""]
[Black ""Duke of Brunswick and Count Isouard""]
[Result ""1-0""]

1.e4 e5 2.Nf3 d6 3.d4 Bg4 4.dxe5 Bxf3 5.Qxf3 dxe5 6.Bc4 Nf6 7.Qb3 Qe7 8.Nc3 c6 9.Bg5 b5 10.Nxb5 cxb5 11.Bxb5+ Nbd7 12.O-O-O Rd8 13.Rxd7 Rxd7 14.Rd1 Qe6 15.Bxd7+ Nxd7 16.Qb8+ Nxb8 17.Rd8# 1-0")
        };
    }
    
    partial void OnSelectedGameChanged(PgnGameInfo? value)
    {
        if (value != null)
        {
            LoadGameContent(value);
        }
        else
        {
            SelectedPgnContent = "";
        }
    }
    
    private async void LoadGameContent(PgnGameInfo gameInfo)
    {
        try
        {
            var gamePath = Path.Combine(LibraryPath, $"{gameInfo.Id}.pgn");
            if (File.Exists(gamePath))
            {
                SelectedPgnContent = await File.ReadAllTextAsync(gamePath);
            }
        }
        catch (Exception ex)
        {
            SelectedPgnContent = $"Error loading game content: {ex.Message}";
            _logService.Error($"Error loading game content: {ex.Message}");
        }
    }
}

public class PgnGameInfo
{
    public string Id { get; set; } = "";
    public string White { get; set; } = "";
    public string Black { get; set; } = "";
    public string Event { get; set; } = "";
    public string Site { get; set; } = "";
    public string Date { get; set; } = "";
    public string Round { get; set; } = "";
    public GameResult Result { get; set; }
    public int MoveCount { get; set; }
    public string Opening { get; set; } = "";
    public DateTime ImportDate { get; set; }
    
    public string ResultString => Result switch
    {
        GameResult.WhiteWins => "1-0",
        GameResult.BlackWins => "0-1", 
        GameResult.Draw => "½-½",
        _ => "*"
    };
    
    public string DisplayName => $"{White} vs {Black} ({Date})";
}
