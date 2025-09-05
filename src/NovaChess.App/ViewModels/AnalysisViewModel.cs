using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaChess.Core;
using NovaChess.Infrastructure.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using CoreColor = NovaChess.Core.Color;

namespace NovaChess.App.ViewModels;

public partial class AnalysisViewModel : ObservableObject
{
    private readonly Arbiter _arbiter;
    private readonly ChessAI _analysisEngine;
    private readonly ILogService _logService;
    
    [ObservableProperty]
    private GameState? _currentPosition;
    
    [ObservableProperty]
    private string _fenString = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    
    [ObservableProperty]
    private string _evaluationText = "Position evaluation will appear here";
    
    [ObservableProperty]
    private double _evaluationScore = 0.0;
    
    [ObservableProperty]
    private string _bestMoveText = "Best move will be shown here";
    
    [ObservableProperty]
    private bool _isAnalyzing = false;
    
    [ObservableProperty]
    private int _analysisDepth = 8;
    
    [ObservableProperty]
    private string _gamePhase = "Opening";
    
    public ObservableCollection<AnalysisLine> TopMoves { get; } = new();
    public ObservableCollection<string> PositionNotes { get; } = new();
    
    public IRelayCommand LoadFenCommand { get; }
    public IRelayCommand AnalyzePositionCommand { get; }
    public IRelayCommand ClearAnalysisCommand { get; }
    public IRelayCommand LoadStartingPositionCommand { get; }
    public IRelayCommand FlipBoardCommand { get; }
    
    public AnalysisViewModel(ILogService logService)
    {
        _logService = logService;
        _arbiter = new Arbiter(new ChessMoveGenerator());
        _analysisEngine = new ChessAI(new ChessMoveGenerator(), skillLevel: 20, maxDepth: 10);
        
        LoadFenCommand = new RelayCommand(LoadFenFromString);
        AnalyzePositionCommand = new AsyncRelayCommand(AnalyzeCurrentPosition);
        ClearAnalysisCommand = new RelayCommand(ClearAnalysis);
        LoadStartingPositionCommand = new RelayCommand(LoadStartingPosition);
        FlipBoardCommand = new RelayCommand(FlipBoard);
        
        LoadStartingPosition();
    }
    
    private void LoadFenFromString()
    {
        try
        {
            var gameState = new GameState();
            gameState.LoadFromFen(FenString);
            CurrentPosition = gameState;
            
            EvaluationText = "Position loaded successfully. Click 'Analyze' to evaluate.";
            ClearAnalysis();
            
            _logService.Information($"Loaded position from FEN: {FenString}");
        }
        catch (Exception ex)
        {
            EvaluationText = $"Invalid FEN string: {ex.Message}";
            MessageBox.Show($"Error loading FEN: {ex.Message}", "Invalid FEN", 
                          MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    
    private async Task AnalyzeCurrentPosition()
    {
        if (CurrentPosition == null) return;
        
        try
        {
            IsAnalyzing = true;
            EvaluationText = "🤔 Analyzing position...";
            BestMoveText = "Calculating best move...";
            
            TopMoves.Clear();
            PositionNotes.Clear();
            
            // Run analysis on background thread
            await Task.Run(() => PerformAnalysis());
            
            _logService.Information($"Analysis completed for position: {FenString}");
        }
        catch (Exception ex)
        {
            EvaluationText = $"Analysis error: {ex.Message}";
            _logService.Error($"Analysis error: {ex.Message}");
        }
        finally
        {
            IsAnalyzing = false;
        }
    }
    
    private void PerformAnalysis()
    {
        if (CurrentPosition == null) return;
        
        // Get legal moves
        var legalMoves = _arbiter.LegalMoves(CurrentPosition);
        
        if (!legalMoves.Any())
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_arbiter.IsCheckmate(CurrentPosition))
                {
                    var winner = CurrentPosition.SideToMove == CoreColor.White ? "Black" : "White";
                    EvaluationText = $"🏆 Checkmate! {winner} wins.";
                    EvaluationScore = CurrentPosition.SideToMove == CoreColor.White ? -999.0 : 999.0;
                }
                else
                {
                    EvaluationText = "🤝 Stalemate - Draw";
                    EvaluationScore = 0.0;
                }
            });
            return;
        }
        
        // Analyze top moves
        var moveAnalyses = new List<(Move move, double score)>();
        
        foreach (var move in legalMoves.Take(10)) // Analyze top 10 moves
        {
            // Simple evaluation without applying the move
            var score = EvaluateMove(CurrentPosition, move);
            moveAnalyses.Add((move, score));
        }
        
        // Sort by score (best moves first)
        var sortedMoves = CurrentPosition.SideToMove == CoreColor.White 
            ? moveAnalyses.OrderByDescending(x => x.score).ToList()
            : moveAnalyses.OrderBy(x => x.score).ToList();
        
        // Update UI on main thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Set evaluation
            var bestScore = sortedMoves.First().score;
            EvaluationScore = bestScore;
            
            var scoreText = bestScore > 0 ? $"+{bestScore:F2}" : bestScore.ToString("F2");
            EvaluationText = $"📊 Evaluation: {scoreText} ({GetEvaluationDescription(bestScore)})";
            
            // Set best move
            var bestMove = sortedMoves.First().move;
            BestMoveText = $"🎯 Best move: {bestMove.From.ToAlgebraic()}-{bestMove.To.ToAlgebraic()}";
            
            // Add top moves
            TopMoves.Clear();
            foreach (var (move, score) in sortedMoves.Take(5))
            {
                TopMoves.Add(new AnalysisLine
                {
                    Move = $"{move.From.ToAlgebraic()}-{move.To.ToAlgebraic()}",
                    Evaluation = score > 0 ? $"+{score:F2}" : score.ToString("F2"),
                    Description = GetMoveDescription(move)
                });
            }
            
            // Add position notes
            AddPositionNotes();
        });
    }
    
    private double EvaluateMove(GameState position, Move move)
    {
        // Simple move evaluation
        double score = 0;
        
        if (move.IsCapture)
        {
            score += GetPieceValue(move.CapturedPiece) * 0.1;
        }
        
        if (move.IsPromotion)
        {
            score += 8.0;
        }
        
        // For now, skip the check evaluation to avoid private field access
        // TODO: Implement check detection through public API
        
        return score;
    }
    
    private int GetPieceValue(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => 1,
            PieceType.Knight => 3,
            PieceType.Bishop => 3,
            PieceType.Rook => 5,
            PieceType.Queen => 9,
            _ => 0
        };
    }
    
    private string GetEvaluationDescription(double score)
    {
        return Math.Abs(score) switch
        {
            < 0.5 => "Equal",
            < 1.0 => "Slight advantage",
            < 2.0 => "Clear advantage", 
            < 4.0 => "Winning advantage",
            _ => "Decisive advantage"
        };
    }
    
    private string GetMoveDescription(Move move)
    {
        if (move.IsCapture && move.IsPromotion)
            return "Capture & Promotion";
        if (move.IsCapture)
            return "Capture";
        if (move.IsPromotion)
            return "Promotion";
        if (move.IsCastling)
            return "Castling";
        return "Quiet move";
    }
    
    private void AddPositionNotes()
    {
        if (CurrentPosition == null) return;
        
        PositionNotes.Clear();
        
        // Basic position analysis
        var whitePieces = 0;
        var blackPieces = 0;
        
        for (int i = 0; i < 64; i++)
        {
            var piece = CurrentPosition.Board64[i];
            if (!piece.IsEmpty)
            {
                if (piece.Color == CoreColor.White)
                    whitePieces++;
                else
                    blackPieces++;
            }
        }
        
        PositionNotes.Add($"Material: {whitePieces} White pieces, {blackPieces} Black pieces");
        
        // Game phase
        var totalPieces = whitePieces + blackPieces;
        GamePhase = totalPieces switch
        {
            > 20 => "Opening",
            > 12 => "Middlegame", 
            _ => "Endgame"
        };
        
        PositionNotes.Add($"Game phase: {GamePhase}");
        
        // Turn to move
        PositionNotes.Add($"Turn: {(CurrentPosition.SideToMove == CoreColor.White ? "White" : "Black")} to move");
        
        // Castling rights
        var castlingRights = new List<string>();
        if (CurrentPosition.WhiteCastleK) castlingRights.Add("White kingside");
        if (CurrentPosition.WhiteCastleQ) castlingRights.Add("White queenside");
        if (CurrentPosition.BlackCastleK) castlingRights.Add("Black kingside");
        if (CurrentPosition.BlackCastleQ) castlingRights.Add("Black queenside");
        
        if (castlingRights.Any())
            PositionNotes.Add($"Castling available: {string.Join(", ", castlingRights)}");
        else
            PositionNotes.Add("No castling rights remaining");
    }
    
    private void ClearAnalysis()
    {
        EvaluationText = "Click 'Analyze Position' to evaluate";
        EvaluationScore = 0.0;
        BestMoveText = "Best move will be shown here";
        TopMoves.Clear();
        PositionNotes.Clear();
    }
    
    private void LoadStartingPosition()
    {
        FenString = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        LoadFenFromString();
    }
    
    private void FlipBoard()
    {
        // TODO: Implement board flipping
        MessageBox.Show("Board flip feature coming soon!", "Info", 
                      MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

public class AnalysisLine
{
    public string Move { get; set; } = "";
    public string Evaluation { get; set; } = "";
    public string Description { get; set; } = "";
}
