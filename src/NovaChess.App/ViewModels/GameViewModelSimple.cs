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
/// Simplified GameViewModel that uses the new chess engine exclusively
/// </summary>
public partial class GameViewModelSimple : ObservableObject
{
    private readonly IEngineService _engineService;
    private readonly ILogService _logService;
    private readonly Arbiter _arbiter;
    
    [ObservableProperty]
    private GameState? _gameState;
    
    [ObservableProperty]
    private bool _isGameActive;
    
    public ObservableCollection<MoveListItem> MoveHistory { get; } = new();
    
    public GameViewModelSimple(IEngineService engineService, ILogService logService)
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
        
        var playedMove = _arbiter.TryPlay(GameState, from, to, promotionTo);
        
        if (playedMove != null)
        {
            System.Diagnostics.Debug.WriteLine($"✅ MOVE SUCCESS: {playedMove}");
            System.Diagnostics.Debug.WriteLine($"New turn: {GameState.SideToMove}");
            
            // Add to move history
            AddToMoveHistory(playedMove);
            
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
    
    private void AddToMoveHistory(Move move)
    {
        // Simple move notation for now
        var moveString = $"{move.From.ToAlgebraic()}-{move.To.ToAlgebraic()}";
        var moveNumber = GameState!.FullmoveNumber;
        var isWhiteMove = GameState.SideToMove == CoreColor.Black; // Move was made by the other side
        
        // Find or create move list item
        var existingItem = MoveHistory.LastOrDefault(m => m.MoveNumber == moveNumber);
        if (existingItem == null)
        {
            existingItem = new MoveListItem { MoveNumber = moveNumber };
            MoveHistory.Add(existingItem);
        }
        
        if (isWhiteMove)
            existingItem.WhiteMove = moveString;
        else
            existingItem.BlackMove = moveString;
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
        
        var legalMoves = _arbiter.LegalMoves(GameState);
        return legalMoves.Count == 0; // No legal moves = checkmate or stalemate
    }
    
    /// <summary>
    /// Get current game status
    /// </summary>
    public string GetGameStatus()
    {
        if (GameState == null) return "No game";
        
        if (IsGameOver())
        {
            // TODO: Implement proper checkmate/stalemate detection
            return "Game Over";
        }
        
        return GameState.SideToMove == CoreColor.White ? "White to move" : "Black to move";
    }
}
