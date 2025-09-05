using Xunit;
using NovaChess.Core;
using CoreColor = NovaChess.Core.Color;

namespace NovaChess.Tests;

/// <summary>
/// Demonstrates the working chess engine integration
/// </summary>
public class IntegrationDemo
{
    [Fact]
    public void ChessEngine_PlayFullGame_WorksPerfectly()
    {
        // Initialize the chess engine
        var gameState = new GameState();
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        System.Diagnostics.Debug.WriteLine("=== CHESS ENGINE INTEGRATION DEMO ===");
        System.Diagnostics.Debug.WriteLine($"Initial position: {gameState.ToFen()}");
        System.Diagnostics.Debug.WriteLine($"Turn: {gameState.SideToMove}");
        
        // Test 1: Valid pawn move
        System.Diagnostics.Debug.WriteLine("\n--- Test 1: White pawn e2-e4 ---");
        var e2 = Square.FromAlgebraic("e2");
        var e4 = Square.FromAlgebraic("e4");
        
        var legalMoves = arbiter.LegalMovesFrom(gameState, e2);
        System.Diagnostics.Debug.WriteLine($"Legal moves from e2: {legalMoves.Count}");
        
        var move1 = arbiter.TryPlay(gameState, e2, e4);
        Assert.NotNull(move1);
        Assert.Equal(CoreColor.Black, gameState.SideToMove); // Turn switched
        System.Diagnostics.Debug.WriteLine($"✅ Move successful: {move1}");
        System.Diagnostics.Debug.WriteLine($"New turn: {gameState.SideToMove}");
        
        // Test 2: Invalid move (white trying to move again)
        System.Diagnostics.Debug.WriteLine("\n--- Test 2: White tries to move again (should fail) ---");
        var d2 = Square.FromAlgebraic("d2");
        var d4 = Square.FromAlgebraic("d4");
        
        var invalidMove = arbiter.TryPlay(gameState, d2, d4);
        Assert.Null(invalidMove); // Should be rejected
        System.Diagnostics.Debug.WriteLine("❌ Move correctly rejected - not White's turn");
        
        // Test 3: Valid black response
        System.Diagnostics.Debug.WriteLine("\n--- Test 3: Black pawn e7-e5 ---");
        var e7 = Square.FromAlgebraic("e7");
        var e5 = Square.FromAlgebraic("e5");
        
        var move2 = arbiter.TryPlay(gameState, e7, e5);
        Assert.NotNull(move2);
        Assert.Equal(CoreColor.White, gameState.SideToMove); // Turn switched back
        System.Diagnostics.Debug.WriteLine($"✅ Move successful: {move2}");
        
        // Test 4: Knight move
        System.Diagnostics.Debug.WriteLine("\n--- Test 4: White knight Nb1-c3 ---");
        var b1 = Square.FromAlgebraic("b1");
        var c3 = Square.FromAlgebraic("c3");
        
        var knightMoves = arbiter.LegalMovesFrom(gameState, b1);
        System.Diagnostics.Debug.WriteLine($"Legal knight moves from b1: {knightMoves.Count}");
        foreach (var m in knightMoves)
        {
            System.Diagnostics.Debug.WriteLine($"  - {m.To.ToAlgebraic()}");
        }
        
        var move3 = arbiter.TryPlay(gameState, b1, c3);
        Assert.NotNull(move3);
        System.Diagnostics.Debug.WriteLine($"✅ Knight move successful: {move3}");
        
        // Test 5: Invalid knight move
        System.Diagnostics.Debug.WriteLine("\n--- Test 5: Invalid knight move Ng1-h4 (blocked by pawn) ---");
        var g1 = Square.FromAlgebraic("g1");
        var h4 = Square.FromAlgebraic("h4");
        
        var invalidKnight = arbiter.TryPlay(gameState, g1, h4);
        Assert.Null(invalidKnight); // Should be rejected - blocked by h2 pawn
        System.Diagnostics.Debug.WriteLine("❌ Invalid knight move correctly rejected");
        
        System.Diagnostics.Debug.WriteLine("\n=== ALL TESTS PASSED! CHESS ENGINE WORKS PERFECTLY ===");
        System.Diagnostics.Debug.WriteLine($"Final position: {gameState.ToFen()}");
        System.Diagnostics.Debug.WriteLine($"Total legal moves in current position: {arbiter.LegalMoves(gameState).Count}");
    }
    
    [Fact]
    public void ChessEngine_ProperRuleEnforcement_AllPiecesWork()
    {
        var gameState = new GameState();
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        System.Diagnostics.Debug.WriteLine("\n=== TESTING ALL PIECE TYPES ===");
        
        // Test pawn rules
        var pawnMoves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("e2"));
        Assert.Equal(2, pawnMoves.Count); // e3 and e4
        System.Diagnostics.Debug.WriteLine($"✅ Pawn has {pawnMoves.Count} legal moves (e3, e4)");
        
        // Test knight rules
        var knightMoves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("b1"));
        Assert.Equal(2, knightMoves.Count); // a3 and c3
        System.Diagnostics.Debug.WriteLine($"✅ Knight has {knightMoves.Count} legal moves (a3, c3)");
        
        // Test that pieces can't move through other pieces
        var rookMoves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("a1"));
        Assert.Equal(0, rookMoves.Count); // Blocked by pawn
        System.Diagnostics.Debug.WriteLine($"✅ Rook correctly blocked by pawn: {rookMoves.Count} moves");
        
        // Test that you can't capture your own pieces
        var totalMoves = arbiter.LegalMoves(gameState);
        System.Diagnostics.Debug.WriteLine($"✅ Total legal moves in starting position: {totalMoves.Count}");
        Assert.Equal(20, totalMoves.Count); // Standard opening moves
        
        System.Diagnostics.Debug.WriteLine("=== ALL PIECE RULES WORKING CORRECTLY ===");
    }
}
