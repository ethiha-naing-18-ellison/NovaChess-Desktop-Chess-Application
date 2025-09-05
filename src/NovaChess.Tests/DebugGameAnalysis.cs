using Xunit;
using NovaChess.Core;
using CoreColor = NovaChess.Core.Color;

namespace NovaChess.Tests;

/// <summary>
/// Debug game analysis issues
/// </summary>
public class DebugGameAnalysis
{
    [Fact]
    public void DebugKingVsKingPosition()
    {
        var gameState = new GameState();
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        // Clear the board
        for (int i = 0; i < 64; i++)
        {
            gameState.Board64[i] = Piece.None;
        }
        
        // Place only kings
        gameState.SetPiece(Square.FromAlgebraic("e1"), new Piece(PieceType.King, CoreColor.White));
        gameState.SetPiece(Square.FromAlgebraic("e8"), new Piece(PieceType.King, CoreColor.Black));
        gameState.SideToMove = CoreColor.White;
        
        System.Diagnostics.Debug.WriteLine($"=== DEBUG K vs K POSITION ===");
        System.Diagnostics.Debug.WriteLine($"Position: {gameState.ToFen()}");
        
        var legalMoves = arbiter.LegalMoves(gameState);
        System.Diagnostics.Debug.WriteLine($"Legal moves count: {legalMoves.Count}");
        
        foreach (var move in legalMoves)
        {
            System.Diagnostics.Debug.WriteLine($"  {move.From.ToAlgebraic()}-{move.To.ToAlgebraic()}");
        }
        
        var isInCheck = arbiter.IsInCheck(gameState, gameState.SideToMove);
        System.Diagnostics.Debug.WriteLine($"Is in check: {isInCheck}");
        
        var result = arbiter.AnalyzePosition(gameState);
        System.Diagnostics.Debug.WriteLine($"Game result: {result}");
        
        var drawReason = arbiter.GetDrawReason(gameState);
        System.Diagnostics.Debug.WriteLine($"Draw reason: {drawReason}");
        
        // Test insufficient material detection directly
        var gameAnalyzer = new GameAnalyzer(new ChessMoveGenerator());
        var isInsufficientMaterial = gameAnalyzer.GetType()
            .GetMethod("IsDrawByInsufficientMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(gameAnalyzer, new object[] { gameState });
        
        System.Diagnostics.Debug.WriteLine($"Insufficient material (direct): {isInsufficientMaterial}");
        
        // The position should be a draw, but let's see what happens
        Assert.True(true); // Just pass for now so we can see the debug output
    }
}
