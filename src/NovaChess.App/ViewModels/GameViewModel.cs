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
    private ChessAI? _chessAI;
    
    [ObservableProperty]
    private GameState? _gameState;
    
    [ObservableProperty]
    private bool _isGameActive;
    
    [ObservableProperty]
    private GameConfiguration? _gameConfiguration;
    
    [ObservableProperty]
    private bool _isAIThinking;
    
    // Backward compatibility property for UI
    public Board? Board => null; // UI will use GameState instead
    
    public ObservableCollection<MoveListItem> MoveHistory { get; } = new();
    
    public GameViewModel(IEngineService engineService, ILogService logService)
    {
        _engineService = engineService;
        _logService = logService;
        _arbiter = new Arbiter(new ChessMoveGenerator());
        _chessAI = null; // Will be initialized when starting a PvC game
        
        // Don't auto-initialize - wait for explicit game start
        // This prevents overriding configurations set by dialogs
        System.Diagnostics.Debug.WriteLine("🎮 GameViewModel created - waiting for game configuration");
    }
    
    private void InitializeNewGame()
    {
        // Default Player vs Player game - only if no game is already active
        if (GameState == null)
        {
            InitializeNewGame(new GameConfiguration { GameMode = GameMode.PlayerVsPlayer });
        }
    }
    
    /// <summary>
    /// Ensure a game is initialized (called by UI when needed)
    /// </summary>
    public void EnsureGameInitialized()
    {
        if (GameState == null)
        {
            System.Diagnostics.Debug.WriteLine("🎮 No game found - initializing default Player vs Player");
            InitializeNewGame();
        }
    }
    
    public void InitializeNewGame(GameConfiguration config)
    {
        GameConfiguration = config;
        GameState = new GameState();
        
        System.Diagnostics.Debug.WriteLine($"🎮 InitializeNewGame called with mode: {config.GameMode}");
        System.Diagnostics.Debug.WriteLine($"🎮 AI Skill: {config.AISkillLevel}, Depth: {config.AISearchDepth}");
        
        // Load custom starting position if specified
        if (!string.IsNullOrEmpty(config.StartingPosition) && 
            config.StartingPosition != "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
        {
            try
            {
                GameState.LoadFromFen(config.StartingPosition);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load FEN: {ex.Message}");
                // Fall back to standard position
                GameState = new GameState();
            }
        }
        
        IsGameActive = true;
        IsAIThinking = false;
        MoveHistory.Clear();
        
        // Initialize AI if needed
        if (config.GameMode == GameMode.PlayerVsComputer)
        {
            _chessAI = new ChessAI(new ChessMoveGenerator(), config.AISkillLevel, config.AISearchDepth);
            System.Diagnostics.Debug.WriteLine($"=== AI INITIALIZED: Skill={config.AISkillLevel}, Depth={config.AISearchDepth} ===");
        }
        else
        {
            _chessAI = null;
        }
        
        System.Diagnostics.Debug.WriteLine($"=== NEW CHESS GAME STARTED: {config.GameMode} ===");
        System.Diagnostics.Debug.WriteLine($"Turn: {GameState.SideToMove}");
        System.Diagnostics.Debug.WriteLine($"Board FEN: {GameState.ToFen()}");
        
        // Notify UI
        OnPropertyChanged(nameof(GameState));
        OnPropertyChanged(nameof(GameConfiguration));
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
            
            // Check if it's AI's turn to move
            if (ShouldAIMakeMove())
            {
                _ = MakeAIMoveAsync(); // Fire and forget
            }
            
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
    /// Check if AI should make a move
    /// </summary>
    private bool ShouldAIMakeMove()
    {
        return GameConfiguration?.GameMode == GameMode.PlayerVsComputer && 
               _chessAI != null && 
               IsGameActive && 
               !IsAIThinking &&
               GameState?.SideToMove == CoreColor.Black; // AI plays as Black
    }
    
    /// <summary>
    /// Make an AI move asynchronously
    /// </summary>
    private async Task MakeAIMoveAsync()
    {
        if (GameState == null || _chessAI == null || !IsGameActive) return;
        
        try
        {
            IsAIThinking = true;
            OnPropertyChanged(nameof(IsAIThinking));
            
            System.Diagnostics.Debug.WriteLine("🤖 AI is thinking...");
            
            // Add a small delay to show thinking indicator
            await Task.Delay(500);
            
            // Get AI move on background thread
            var aiMove = await Task.Run(() => _chessAI.GetBestMove(GameState));
            
            if (aiMove != null && IsGameActive)
            {
                System.Diagnostics.Debug.WriteLine($"🤖 AI selected move: {aiMove}");
                
                // Store move number before making the move
                var moveNumberBeforeMove = GameState.FullmoveNumber;
                
                // Apply the AI move using the Arbiter
                var success = _arbiter.TryPlay(GameState, aiMove);
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine("🤖 AI move was rejected by Arbiter");
                    return;
                }
                
                // Add to move history
                AddToMoveHistory(aiMove, moveNumberBeforeMove);
                
                // Notify UI of changes
                OnPropertyChanged(nameof(GameState));
                
                _logService.Information($"AI Move: {aiMove}");
                System.Diagnostics.Debug.WriteLine($"✅ AI MOVE SUCCESS: {aiMove}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("🤖 AI could not find a move");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ AI ERROR: {ex.Message}");
            _logService.Error($"AI Error: {ex.Message}");
        }
        finally
        {
            IsAIThinking = false;
            OnPropertyChanged(nameof(IsAIThinking));
        }
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
