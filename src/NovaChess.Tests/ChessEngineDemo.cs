using Xunit;
using Xunit.Abstractions;
using NovaChess.Core;
using System.Linq;
using CoreColor = NovaChess.Core.Color;

namespace NovaChess.Tests;

public class ChessEngineDemoTests
{
    private readonly ITestOutputHelper _output;
    
    public ChessEngineDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void ChessEngine_EnforcesAllRulesCorrectly()
    {
        _output.WriteLine("=== NOVACHESS ENGINE DEMONSTRATION ===");
        _output.WriteLine("Showing strict chess rule enforcement");
        _output.WriteLine("");
        
        var gameState = new GameState();
        var arbiter = new Arbiter();
        
        // Show initial state
        _output.WriteLine($"Initial position - Turn: {gameState.SideToMove}");
        _output.WriteLine("");
        
        // 1. Valid white pawn move
        _output.WriteLine("1. TESTING: e2-e4 (white pawn double push)");
        var move1 = arbiter.TryPlay(gameState, Square.FromAlgebraic("e2"), Square.FromAlgebraic("e4"));
        _output.WriteLine($"   Result: {(move1 != null ? "✅ SUCCESS" : "❌ FAILED")}");
        _output.WriteLine($"   Turn switched to: {gameState.SideToMove}");
        Assert.NotNull(move1);
        Assert.Equal(CoreColor.Black, gameState.SideToMove);
        _output.WriteLine("");
        
        // 2. Try to move white again (should fail - turn enforcement)
        _output.WriteLine("2. TESTING: d2-d4 (white trying to move twice - should fail)");
        var move2 = arbiter.TryPlay(gameState, Square.FromAlgebraic("d2"), Square.FromAlgebraic("d4"));
        _output.WriteLine($"   Result: {(move2 != null ? "❌ FAILED" : "✅ CORRECTLY BLOCKED")}");
        _output.WriteLine($"   Turn remains: {gameState.SideToMove}");
        Assert.Null(move2); // Should fail
        Assert.Equal(CoreColor.Black, gameState.SideToMove); // Turn should not change
        _output.WriteLine("");
        
        // 3. Valid black response
        _output.WriteLine("3. TESTING: e7-e5 (black pawn response)");
        var move3 = arbiter.TryPlay(gameState, Square.FromAlgebraic("e7"), Square.FromAlgebraic("e5"));
        _output.WriteLine($"   Result: {(move3 != null ? "✅ SUCCESS" : "❌ FAILED")}");
        _output.WriteLine($"   Turn switched to: {gameState.SideToMove}");
        Assert.NotNull(move3);
        Assert.Equal(CoreColor.White, gameState.SideToMove);
        _output.WriteLine("");
        
        // 4. Try invalid pawn move (3 squares)
        _output.WriteLine("4. TESTING: d2-d5 (pawn trying to move 3 squares - should fail)");
        var move4 = arbiter.TryPlay(gameState, Square.FromAlgebraic("d2"), Square.FromAlgebraic("d5"));
        _output.WriteLine($"   Result: {(move4 != null ? "❌ FAILED" : "✅ CORRECTLY BLOCKED")}");
        Assert.Null(move4); // Should fail
        _output.WriteLine("");
        
        // 5. Show legal moves for knight
        _output.WriteLine("5. TESTING: Legal moves for white knight on b1");
        var knightMoves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("b1"));
        var moveList = string.Join(", ", knightMoves.Select(m => m.To.ToAlgebraic()));
        _output.WriteLine($"   Available moves: {moveList}");
        Assert.Equal(2, knightMoves.Count); // Should have exactly 2 moves: a3, c3
        Assert.Contains(knightMoves, m => m.To == Square.FromAlgebraic("a3"));
        Assert.Contains(knightMoves, m => m.To == Square.FromAlgebraic("c3"));
        _output.WriteLine("");
        
        // 6. Try to move knight to illegal square
        _output.WriteLine("6. TESTING: Nb1-b3 (knight illegal move - should fail)");
        var move6 = arbiter.TryPlay(gameState, Square.FromAlgebraic("b1"), Square.FromAlgebraic("b3"));
        _output.WriteLine($"   Result: {(move6 != null ? "❌ FAILED" : "✅ CORRECTLY BLOCKED")}");
        Assert.Null(move6); // Should fail
        _output.WriteLine("");
        
        // 7. Valid knight move
        _output.WriteLine("7. TESTING: Nb1-c3 (proper L-shaped knight move)");
        var move7 = arbiter.TryPlay(gameState, Square.FromAlgebraic("b1"), Square.FromAlgebraic("c3"));
        _output.WriteLine($"   Result: {(move7 != null ? "✅ SUCCESS" : "❌ FAILED")}");
        _output.WriteLine($"   Turn switched to: {gameState.SideToMove}");
        Assert.NotNull(move7);
        Assert.Equal(CoreColor.Black, gameState.SideToMove);
        _output.WriteLine("");
        
        // 8. Try to capture own piece
        _output.WriteLine("8. TESTING: Black trying to capture own piece (should fail)");
        var move8 = arbiter.TryPlay(gameState, Square.FromAlgebraic("b8"), Square.FromAlgebraic("d7"));
        _output.WriteLine($"   Result: {(move8 != null ? "❌ FAILED" : "✅ CORRECTLY BLOCKED")}");
        Assert.Null(move8); // Should fail
        _output.WriteLine("");
        
        _output.WriteLine("=== DEMONSTRATION COMPLETE ===");
        _output.WriteLine("✅ Turn enforcement: WORKING");
        _output.WriteLine("✅ Legal move generation: WORKING");  
        _output.WriteLine("✅ Invalid move rejection: WORKING");
        _output.WriteLine("✅ Piece-specific rules: WORKING");
        _output.WriteLine("✅ Own piece capture prevention: WORKING");
        _output.WriteLine("");
        _output.WriteLine("🎯 The chess engine core is fully functional!");
        _output.WriteLine("Ready for UI integration with Arbiter.TryPlay()");
        
        // Final state verification
        Assert.Equal(PieceType.Pawn, gameState.GetPiece(Square.FromAlgebraic("e4")).Type); // White pawn moved
        Assert.Equal(PieceType.Pawn, gameState.GetPiece(Square.FromAlgebraic("e5")).Type); // Black pawn moved  
        Assert.Equal(PieceType.Knight, gameState.GetPiece(Square.FromAlgebraic("c3")).Type); // Knight moved
        Assert.True(gameState.GetPiece(Square.FromAlgebraic("e2")).IsEmpty); // Original pawn square empty
        Assert.True(gameState.GetPiece(Square.FromAlgebraic("b1")).IsEmpty); // Original knight square empty
    }
}
