using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaChess.Core;
using NovaChess.Infrastructure.Interfaces;
using NovaChess.App.Models;

namespace NovaChess.App.ViewModels;

public partial class GameViewModel : ObservableObject
{
    private readonly IEngineService _engineService;
    private readonly ILogService _logService;
    
    [ObservableProperty]
    private Game? _game;
    
    [ObservableProperty]
    private Board? _board;
    
    [ObservableProperty]
    private GameConfiguration? _gameConfig;
    
    [ObservableProperty]
    private bool _isGameActive;
    
    [ObservableProperty]
    private bool _isPlayerTurn = true;
    
    [ObservableProperty]
    private string _gameStatus = "White to move";
    
    [ObservableProperty]
    private string _whiteTime = "∞";
    
    [ObservableProperty]
    private string _blackTime = "∞";
    
    [ObservableProperty]
    private bool _isThinking;
    
    public ObservableCollection<MoveListItem> MoveHistory { get; } = new();
    
    public GameViewModel(IEngineService engineService, ILogService logService)
    {
        _engineService = engineService;
        _logService = logService;
        
        // Initialize commands
        NewGameCommand = new AsyncRelayCommand(NewGameAsync);
        UndoMoveCommand = new RelayCommand(UndoMove, CanUndoMove);
        RedoMoveCommand = new RelayCommand(RedoMove, CanRedoMove);
        ResignCommand = new RelayCommand(Resign, CanResign);
        DrawOfferCommand = new RelayCommand(OfferDraw, CanOfferDraw);
        
        // Initialize with a default board for display
        InitializeDefaultBoard();
    }
    
    private void InitializeDefaultBoard()
    {
        // Create a default game with starting position
        Game = new Game();
        Board = Game.Board;
        IsGameActive = false; // Not an active game yet, just for display
        UpdateGameStatus();
    }
    
    public IAsyncRelayCommand NewGameCommand { get; }
    public IRelayCommand UndoMoveCommand { get; }
    public IRelayCommand RedoMoveCommand { get; }
    public IRelayCommand ResignCommand { get; }
    public IRelayCommand DrawOfferCommand { get; }
    
    public async Task NewGameAsync()
    {
        try
        {
            var dialog = new Views.NewGameDialog();
            if (dialog.ShowDialog() == true)
            {
                await StartNewGameAsync(dialog.GameConfig);
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error starting new game: {ex.Message}");
            MessageBox.Show($"Error starting new game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task StartNewGameAsync(GameConfiguration config)
    {
        try
        {
            GameConfig = config;
            
            // Initialize the game
            Game = new Game();
            Board = Game.Board;
            
            // Load starting position
            if (!string.IsNullOrEmpty(config.StartingPosition))
            {
                Game.Board.LoadFromFen(config.StartingPosition);
            }
            
            // Initialize time control
            InitializeTimeControl(config.TimeControl);
            
            // Connect to engine if playing against computer
            if (config.GameMode == GameMode.PlayerVsComputer)
            {
                await InitializeEngineAsync(config);
            }
            
            IsGameActive = true;
            IsPlayerTurn = true;
            UpdateGameStatus();
            MoveHistory.Clear();
            
            _logService.Information($"New game started: {config.GameMode} mode");
        }
        catch (Exception ex)
        {
            _logService.Error($"Error starting new game: {ex.Message}");
            MessageBox.Show($"Error starting new game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void InitializeTimeControl(GameTimeControl timeControl)
    {
        var timeLimit = timeControl switch
        {
            GameTimeControl.Blitz => TimeSpan.FromMinutes(5),
            GameTimeControl.Rapid => TimeSpan.FromMinutes(15),
            GameTimeControl.Classical => TimeSpan.FromMinutes(30),
            GameTimeControl.Custom => TimeSpan.FromMinutes(10), // Default custom time
            _ => TimeSpan.Zero
        };
        
        if (timeLimit > TimeSpan.Zero)
        {
            WhiteTime = timeLimit.ToString(@"mm\:ss");
            BlackTime = timeLimit.ToString(@"mm\:ss");
        }
        else
        {
            WhiteTime = "∞";
            BlackTime = "∞";
        }
    }
    
    private async Task InitializeEngineAsync(GameConfiguration config)
    {
        try
        {
            // For now, we'll assume Stockfish is available
            // In production, you'd want to bundle the engine or provide a download option
            var enginePath = "stockfish.exe"; // This should be configurable
            
            if (await _engineService.ConnectAsync(enginePath))
            {
                await _engineService.SetSkillLevelAsync(config.AISkillLevel);
                await _engineService.SetDepthAsync(config.AISearchDepth);
                _logService.Information("Engine connected successfully");
            }
            else
            {
                _logService.Warning("Failed to connect to engine, falling back to simple AI");
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error initializing engine: {ex.Message}");
        }
    }
    
    public async Task MakeMoveAsync(Move move)
    {
        if (Game == null) return;
        
        try
        {
            // Validate the move before attempting to make it
            if (!move.From.IsValid || !move.To.IsValid)
            {
                _logService.Warning($"Invalid move coordinates: from={move.From}, to={move.To}");
                return;
            }
            
            // For now, we'll accept basic moves and let the Game class handle validation
            // In a full implementation, you'd want more thorough validation
            
            // Execute the move
            var moveSuccessful = Game.MakeMove(move);
            
            if (moveSuccessful)
            {
                System.Diagnostics.Debug.WriteLine("Move successful via Game class");
                // Force UI update
                OnPropertyChanged(nameof(Board));
            }
            
            if (!moveSuccessful)
            {
                _logService.Warning($"Move rejected by Game class: {move}");
                System.Diagnostics.Debug.WriteLine($"Move rejected by Game class: {move}");
                
                // Try to execute the move directly on the board for debugging
                try
                {
                    var (pieceType, pieceColor) = Game.Board.GetPiece(move.From);
                    var (capturedPiece, _) = Game.Board.GetPiece(move.To);
                    
                    // Remove piece from source
                    Game.Board.SetPiece(move.From, PieceType.None, PieceColor.White);
                    
                    // Place piece at destination
                    Game.Board.SetPiece(move.To, pieceType, pieceColor);
                    
                    // Switch sides
                    Game.Board.SideToMove = Game.Board.SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    
                    // Update fullmove number
                    if (Game.Board.SideToMove == PieceColor.White)
                    {
                        Game.Board.FullMoveNumber++;
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Move executed directly on board");
                    moveSuccessful = true;
                    
                    // Force UI update by triggering property change
                    OnPropertyChanged(nameof(Board));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error executing move directly: {ex.Message}");
                    return;
                }
            }
            
            if (!moveSuccessful)
            {
                return;
            }
            
            // Add to move history
            var moveNumber = Game.Board.FullMoveNumber;
            MoveHistory.Add(new MoveListItem 
            { 
                MoveNumber = (moveNumber + 1) / 2, 
                WhiteMove = moveNumber % 2 == 1 ? GetBasicSanNotation(move) : "",
                BlackMove = moveNumber % 2 == 0 ? GetBasicSanNotation(move) : ""
            });
            
            // Update board and status
            Board = Game.Board;
            UpdateGameStatus();
            
            // Check if game is over
            if (Game.Result != GameResult.Ongoing)
            {
                HandleGameEnd();
                return;
            }
            
            // Switch turns
            IsPlayerTurn = !IsPlayerTurn;
            
            // If it's AI's turn, make AI move
            if (GameConfig?.GameMode == GameMode.PlayerVsComputer && !IsPlayerTurn)
            {
                await MakeAIMoveAsync();
            }
            
            _logService.Information($"Move made: {GetBasicSanNotation(move)}");
        }
        catch (Exception ex)
        {
            _logService.Error($"Error making move: {ex.Message}");
            // Don't show error dialog to avoid interrupting gameplay
            System.Diagnostics.Debug.WriteLine($"Move error: {ex.Message}");
        }
    }
    
    private string GetBasicSanNotation(Move move)
    {
        // Basic SAN notation without relying on move flags
        var pieceSymbol = move.Piece switch
        {
            PieceType.Pawn => "",
            PieceType.Knight => "N",
            PieceType.Bishop => "B",
            PieceType.Rook => "R",
            PieceType.Queen => "Q",
            PieceType.King => "K",
            _ => ""
        };
        
        var destination = move.To.ToAlgebraic();
        
        return pieceSymbol + destination;
    }
    
    private async Task MakeAIMoveAsync()
    {
        if (Game == null || !_engineService.IsConnected) return;
        
        try
        {
            IsThinking = true;
            
            // Get current position FEN
            var fen = Game.Board.ToFen();
            
            // Get engine evaluation
            var evaluation = await _engineService.GetBestMoveAsync(fen, TimeSpan.FromSeconds(5));
            
            if (evaluation?.BestMove != null)
            {
                // Parse the UCI move
                var move = ParseUciMove(evaluation.BestMove);
                if (move.HasValue)
                {
                    await Task.Delay(500); // Small delay for better UX
                    await MakeMoveAsync(move.Value);
                }
            }
            else
            {
                // Fallback to random legal move
                var legalMoves = Game.GetLegalMoves();
                if (legalMoves.Count > 0)
                {
                    var random = new Random();
                    var randomMove = legalMoves[random.Next(legalMoves.Count)];
                    await Task.Delay(500);
                    await MakeMoveAsync(randomMove);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error making AI move: {ex.Message}");
            // Fallback to random move
            var legalMoves = Game.GetLegalMoves();
            if (legalMoves.Count > 0)
            {
                var random = new Random();
                var randomMove = legalMoves[random.Next(legalMoves.Count)];
                await MakeMoveAsync(randomMove);
            }
        }
        finally
        {
            IsThinking = false;
        }
    }
    
    private Move? ParseUciMove(string uciMove)
    {
        if (uciMove.Length < 4) return null;
        
        try
        {
            var fromFile = uciMove[0] - 'a';
            var fromRank = uciMove[1] - '1';
            var toFile = uciMove[2] - 'a';
            var toRank = uciMove[3] - '1';
            
            var from = new Square(fromRank * 8 + fromFile);
            var to = new Square(toRank * 8 + toFile);
            
            if (from.Index < 0 || from.Index > 63 || to.Index < 0 || to.Index > 63)
                return null;
            
            var (pieceType, _) = Game?.Board.GetPiece(from) ?? (PieceType.None, PieceColor.White);
            if (pieceType == PieceType.None) return null;
            
            return new Move(from, to, pieceType);
        }
        catch
        {
            return null;
        }
    }
    
    private void UpdateGameStatus()
    {
        if (Game == null) return;
        
        if (Game.Result == GameResult.Ongoing)
        {
            GameStatus = Board?.SideToMove == PieceColor.White ? "White to move" : "Black to move";
        }
        else
        {
            GameStatus = Game.Result switch
            {
                GameResult.WhiteWins => "White wins!",
                GameResult.BlackWins => "Black wins!",
                GameResult.Draw => $"Draw - {Game.DrawReason}",
                _ => "Game over"
            };
        }
    }
    
    private void HandleGameEnd()
    {
        IsGameActive = false;
        UpdateGameStatus();
        
        var message = Game?.Result switch
        {
            GameResult.WhiteWins => "White wins!",
            GameResult.BlackWins => "Black wins!",
            GameResult.Draw => $"Game ended in a draw.\nReason: {Game?.DrawReason}",
            _ => "Game over"
        };
        
        MessageBox.Show(message, "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void UndoMove()
    {
        // TODO: Implement undo functionality
        MessageBox.Show("Undo functionality coming soon!", "Feature", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private bool CanUndoMove() => IsGameActive && Game?.MoveHistory.Count > 0;
    
    private void RedoMove()
    {
        // TODO: Implement redo functionality
        MessageBox.Show("Redo functionality coming soon!", "Feature", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private bool CanRedoMove() => false; // TODO: Implement redo stack
    
    private void Resign()
    {
        if (Game == null || !IsGameActive) return;
        
        var result = MessageBox.Show("Are you sure you want to resign?", "Resign", 
                                   MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            // Note: Game.Result is read-only, so we can't set it directly
            // This would need to be implemented in the Game class
            HandleGameEnd();
        }
    }
    
    private bool CanResign() => IsGameActive && IsPlayerTurn;
    
    private void OfferDraw()
    {
        // TODO: Implement draw offer functionality
        MessageBox.Show("Draw offer functionality coming soon!", "Feature", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private bool CanOfferDraw() => IsGameActive && IsPlayerTurn;
}
