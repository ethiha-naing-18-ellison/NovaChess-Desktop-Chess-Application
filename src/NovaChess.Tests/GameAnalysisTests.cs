using Xunit;
using NovaChess.Core;
using CoreColor = NovaChess.Core.Color;

namespace NovaChess.Tests;

/// <summary>
/// Tests for checkmate, stalemate, and draw detection
/// </summary>
public class GameAnalysisTests
{
    private readonly Arbiter _arbiter;
    
    public GameAnalysisTests()
    {
        _arbiter = new Arbiter(new ChessMoveGenerator());
    }
    
    [Fact]
    public void StartingPosition_IsOngoing()
    {
        var gameState = new GameState();
        
        var result = _arbiter.AnalyzePosition(gameState);
        var status = _arbiter.GetGameStatus(gameState);
        
        Assert.Equal(GameResult.Ongoing, result);
        Assert.Equal("White to move", status);
        Assert.False(_arbiter.IsGameOver(gameState));
        Assert.False(_arbiter.IsCheckmate(gameState));
        Assert.False(_arbiter.IsStalemate(gameState));
    }
    
    [Fact]
    public void FoolsMatePosition_IsCheckmate()
    {
        // Fool's mate: 1.f3 e5 2.g4 Qh4# (fastest checkmate)
        var gameState = new GameState();
        
        // Apply moves manually to reach fool's mate position
        // 1.f3
        _arbiter.TryPlay(gameState, Square.FromAlgebraic("f2"), Square.FromAlgebraic("f3"));
        // 1...e5
        _arbiter.TryPlay(gameState, Square.FromAlgebraic("e7"), Square.FromAlgebraic("e5"));
        // 2.g4
        _arbiter.TryPlay(gameState, Square.FromAlgebraic("g2"), Square.FromAlgebraic("g4"));
        // 2...Qh4#
        _arbiter.TryPlay(gameState, Square.FromAlgebraic("d8"), Square.FromAlgebraic("h4"));
        
        var result = _arbiter.AnalyzePosition(gameState);
        var status = _arbiter.GetGameStatus(gameState);
        
        Assert.Equal(GameResult.BlackWins, result);
        Assert.Contains("checkmate", status.ToLower());
        Assert.True(_arbiter.IsGameOver(gameState));
        Assert.True(_arbiter.IsCheckmate(gameState));
        Assert.False(_arbiter.IsStalemate(gameState));
    }
    
    [Fact]
    public void StalematePosition_IsDraw()
    {
        // Create a simple stalemate position: King vs King + Queen
        var gameState = new GameState();
        gameState.LoadFromFen("8/8/8/8/8/5Q2/7K/7k b - - 0 1");
        
        var result = _arbiter.AnalyzePosition(gameState);
        var status = _arbiter.GetGameStatus(gameState);
        var drawReason = _arbiter.GetDrawReason(gameState);
        
        Assert.Equal(GameResult.Draw, result);
        Assert.Contains("stalemate", status.ToLower());
        Assert.Equal(DrawReason.Stalemate, drawReason);
        Assert.True(_arbiter.IsGameOver(gameState));
        Assert.False(_arbiter.IsCheckmate(gameState));
        Assert.True(_arbiter.IsStalemate(gameState));
    }
    
    [Fact]
    public void InsufficientMaterial_KingVsKing_IsDraw()
    {
        // King vs King
        var gameState = new GameState();
        gameState.LoadFromFen("8/8/8/8/8/8/4K3/4k3 w - - 0 1");
        
        var result = _arbiter.AnalyzePosition(gameState);
        var drawReason = _arbiter.GetDrawReason(gameState);
        
        Assert.Equal(GameResult.Draw, result);
        Assert.Equal(DrawReason.InsufficientMaterial, drawReason);
        Assert.True(_arbiter.IsGameOver(gameState));
    }
    
    [Fact]
    public void InsufficientMaterial_KingVsKingBishop_IsDraw()
    {
        // King vs King + Bishop
        var gameState = new GameState();
        gameState.LoadFromFen("8/8/8/8/8/2B5/4K3/4k3 w - - 0 1");
        
        var result = _arbiter.AnalyzePosition(gameState);
        var drawReason = _arbiter.GetDrawReason(gameState);
        
        Assert.Equal(GameResult.Draw, result);
        Assert.Equal(DrawReason.InsufficientMaterial, drawReason);
        Assert.True(_arbiter.IsGameOver(gameState));
    }
    
    [Fact]
    public void InsufficientMaterial_KingVsKingKnight_IsDraw()
    {
        // King vs King + Knight
        var gameState = new GameState();
        gameState.LoadFromFen("8/8/8/8/8/2N5/4K3/4k3 w - - 0 1");
        
        var result = _arbiter.AnalyzePosition(gameState);
        var drawReason = _arbiter.GetDrawReason(gameState);
        
        Assert.Equal(GameResult.Draw, result);
        Assert.Equal(DrawReason.InsufficientMaterial, drawReason);
        Assert.True(_arbiter.IsGameOver(gameState));
    }
    
    [Fact]
    public void SufficientMaterial_KingQueenVsKing_IsOngoing()
    {
        // King + Queen vs King (sufficient material)
        var gameState = new GameState();
        gameState.LoadFromFen("8/8/8/8/8/2Q5/4K3/4k3 w - - 0 1");
        
        var result = _arbiter.AnalyzePosition(gameState);
        
        Assert.Equal(GameResult.Ongoing, result);
        Assert.False(_arbiter.IsGameOver(gameState));
    }
    
    [Fact]
    public void FiftyMoveRule_IsDraw()
    {
        // Position with 50+ moves without pawn move or capture
        var gameState = new GameState();
        gameState.LoadFromFen("8/8/8/8/8/2Q5/4K3/4k3 w - - 100 50");
        
        var result = _arbiter.AnalyzePosition(gameState);
        var drawReason = _arbiter.GetDrawReason(gameState);
        
        Assert.Equal(GameResult.Draw, result);
        Assert.Equal(DrawReason.FiftyMoveRule, drawReason);
        Assert.True(_arbiter.IsGameOver(gameState));
    }
    
    [Fact]
    public void GameInProgress_WhiteInCheck_CorrectStatus()
    {
        // White king in check but not checkmate
        var gameState = new GameState();
        gameState.LoadFromFen("8/8/8/8/8/8/4K3/4k2r w - - 0 1");
        
        var result = _arbiter.AnalyzePosition(gameState);
        var status = _arbiter.GetGameStatus(gameState);
        
        Assert.Equal(GameResult.Ongoing, result);
        Assert.Contains("White is in check", status);
        Assert.False(_arbiter.IsGameOver(gameState));
        Assert.False(_arbiter.IsCheckmate(gameState));
    }
    
    [Fact]
    public void BackRankMate_IsCheckmate()
    {
        // Classic back rank mate
        var gameState = new GameState();
        gameState.LoadFromFen("6rk/6pp/8/8/8/8/6PP/6RK b - - 0 1");
        
        var result = _arbiter.AnalyzePosition(gameState);
        
        Assert.Equal(GameResult.WhiteWins, result);
        Assert.True(_arbiter.IsCheckmate(gameState));
        Assert.True(_arbiter.IsGameOver(gameState));
    }
    
    [Fact]
    public void LegalMovesAvailable_NotCheckmate()
    {
        // King in check but has escape squares
        var gameState = new GameState();
        gameState.LoadFromFen("8/8/8/8/8/8/3K4/3kr3 w - - 0 1");
        
        var result = _arbiter.AnalyzePosition(gameState);
        
        Assert.Equal(GameResult.Ongoing, result);
        Assert.False(_arbiter.IsCheckmate(gameState));
        Assert.False(_arbiter.IsStalemate(gameState));
        Assert.False(_arbiter.IsGameOver(gameState));
    }
}
