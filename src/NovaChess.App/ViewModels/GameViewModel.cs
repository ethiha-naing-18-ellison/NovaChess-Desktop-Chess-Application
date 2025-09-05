using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaChess.Core;
using NovaChess.Infrastructure.Interfaces;
using NovaChess.App.Models;
using CoreColor = NovaChess.Core.Color;

namespace NovaChess.App.ViewModels;

/// <summary>
/// GameViewModel that uses the new chess engine exclusively
/// </summary>
public partial class GameViewModel : ObservableObject
{
    private readonly IEngineService _engineService;
    private readonly ILogService _logService;
    private readonly Arbiter _arbiter;
    
    [ObservableProperty]
    private GameState? _gameState;
    
    [ObservableProperty]
    private bool _isGameActive;
    
    // Backward compatibility property for UI
    public Board? Board => null; // UI will use GameState instead
    
    public ObservableCollection<MoveListItem> MoveHistory { get; } = new();
    
    public GameViewModel(IEngineService engineService, ILogService logService)
    {
        _engineService = engineService;
        _logService = logService;
        _arbiter = new Arbiter(new ChessMoveGenerator());
        
        // Initialize with starting position
        InitializeNewGame();
    }
    
    private void InitializeNewGame()
    {
        GameState = new GameState();
        IsGameActive = true;
        MoveHistory.Clear();
        
        System.Diagnostics.Debug.WriteLine("=== NEW CHESS GAME STARTED ===");
        System.Diagnostics.Debug.WriteLine($"Turn: {GameState.SideToMove}");
        System.Diagnostics.Debug.WriteLine($"Board FEN: {GameState.ToFen()}");
    }
    
    /// <summary>
    /// Try to make a move using the new chess engine
    /// </summary>
    public bool TryMakeMove(Square from, Square to, PieceType promotionTo = PieceType.Queen)
    {
        if (GameState == null || !IsGameActive) return false;
        
        System.Diagnostics.Debug.WriteLine($"=== UI REQUESTING MOVE ===");
        System.Diagnostics.Debug.WriteLine($"From: {from} To: {to}");
        System.Diagnostics.Debug.WriteLine($"Current turn: {GameState.SideToMove}");
        
        // FIXED: Store move number BEFORE making the move
        var moveNumberBeforeMove = GameState.FullmoveNumber;
        var playerMoving = GameState.SideToMove;
        
        var playedMove = _arbiter.TryPlay(GameState, from, to, promotionTo);
        
        if (playedMove != null)
        {
            System.Diagnostics.Debug.WriteLine($"✅ MOVE SUCCESS: {playedMove}");
            System.Diagnostics.Debug.WriteLine($"New turn: {GameState.SideToMove}");
            
            // Add to move history with correct move number
            AddToMoveHistory(playedMove, moveNumberBeforeMove);
            
            // Notify UI of changes
            OnPropertyChanged(nameof(GameState));
            
            _logService.Information($"Move: {playedMove}");
            return true;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"❌ MOVE REJECTED");
            _logService.Warning($"Invalid move: {from}-{to}");
            return false;
        }
    }
    
    /// <summary>
    /// Get legal moves for a square
    /// </summary>
    public IReadOnlyList<Move> GetLegalMovesFrom(Square from)
    {
        if (GameState == null) return new List<Move>();
        return _arbiter.LegalMovesFrom(GameState, from);
    }
    
    /// <summary>
    /// Get all legal moves for current position
    /// </summary>
    public IReadOnlyList<Move> GetAllLegalMoves()
    {
        if (GameState == null) return new List<Move>();
        return _arbiter.LegalMoves(GameState);
    }
    
    private void AddToMoveHistory(Move move, int moveNumber)
    {
        // Simple move notation for now
        var moveString = $"{move.From.ToAlgebraic()}-{move.To.ToAlgebraic()}";
        
        // FIXED: Determine who made the move based on the move's color, not current turn
        var isWhiteMove = move.MovingColor == CoreColor.White;
        
        System.Diagnostics.Debug.WriteLine($"📝 Adding to history: Move #{moveNumber}, {(isWhiteMove ? "White" : "Black")}: {moveString}");
        System.Diagnostics.Debug.WriteLine($"📝 Move.MovingColor = {move.MovingColor}");
        System.Diagnostics.Debug.WriteLine($"📝 GameState.SideToMove = {GameState.SideToMove}");
        System.Diagnostics.Debug.WriteLine($"📝 GameState.FullmoveNumber = {GameState.FullmoveNumber}");
        
        // Find or create move list item
        var existingItem = MoveHistory.LastOrDefault(m => m.MoveNumber == moveNumber);
        if (existingItem == null)
        {
            existingItem = new MoveListItem { MoveNumber = moveNumber };
            MoveHistory.Add(existingItem);
            System.Diagnostics.Debug.WriteLine($"📝 Created new move list item #{moveNumber}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"📝 Using existing move list item #{moveNumber}");
        }
        
        if (isWhiteMove)
        {
            existingItem.WhiteMove = moveString;
            System.Diagnostics.Debug.WriteLine($"📝 SET WHITE MOVE: {moveString}");
        }
        else
        {
            existingItem.BlackMove = moveString;
            System.Diagnostics.Debug.WriteLine($"📝 SET BLACK MOVE: {moveString}");
        }
        
        System.Diagnostics.Debug.WriteLine($"📝 Current MoveHistory count: {MoveHistory.Count}");
        System.Diagnostics.Debug.WriteLine($"📝 Item #{moveNumber}: White='{existingItem.WhiteMove}', Black='{existingItem.BlackMove}'");
    }
    
    /// <summary>
    /// Start a new game
    /// </summary>
    public void NewGame()
    {
        InitializeNewGame();
    }
    
    /// <summary>
    /// Check if the game is over
    /// </summary>
    public bool IsGameOver()
    {
        if (GameState == null) return false;
        return _arbiter.IsGameOver(GameState);
    }
    
    /// <summary>
    /// Get current game status with full analysis
    /// </summary>
    public string GetGameStatus()
    {
        if (GameState == null) return "No game";
        return _arbiter.GetGameStatus(GameState);
    }
    
    /// <summary>
    /// Get the current game result
    /// </summary>
    public GameResult GetGameResult()
    {
        if (GameState == null) return GameResult.Ongoing;
        return _arbiter.AnalyzePosition(GameState);
    }
    
    /// <summary>
    /// Check if current position is checkmate
    /// </summary>
    public bool IsCheckmate()
    {
        if (GameState == null) return false;
        return _arbiter.IsCheckmate(GameState);
    }
    
    /// <summary>
    /// Check if current position is stalemate
    /// </summary>
    public bool IsStalemate()
    {
        if (GameState == null) return false;
        return _arbiter.IsStalemate(GameState);
    }
    
    /// <summary>
    /// Get draw reason if game is drawn
    /// </summary>
    public DrawReason GetDrawReason()
    {
        if (GameState == null) return DrawReason.None;
        return _arbiter.GetDrawReason(GameState);
    }
    
    /// <summary>
    /// Backward compatibility method for UI
    /// </summary>
    public async Task MakeMoveAsync(Move move)
    {
        TryMakeMove(move.From, move.To);
        await Task.CompletedTask;
    }
}
