using NovaChess.Core;
using Xunit;
using System.Linq;

namespace NovaChess.Tests;

public class MoveValidationTest
{
    [Fact]
    public void TestPawnMovesAfterOpening()
    {
        // Test pawn moves after some opening moves
        var gameState = new GameState();
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        // Make some opening moves: 1.e4 e5 2.Nf3 Nc6
        arbiter.TryPlay(gameState, Square.FromAlgebraic("e2"), Square.FromAlgebraic("e4"));
        arbiter.TryPlay(gameState, Square.FromAlgebraic("e7"), Square.FromAlgebraic("e5"));
        arbiter.TryPlay(gameState, Square.FromAlgebraic("g1"), Square.FromAlgebraic("f3"));
        arbiter.TryPlay(gameState, Square.FromAlgebraic("b8"), Square.FromAlgebraic("c6"));
        
        // Now test specific pieces
        
        // White pawn on d2 should still have 2 moves
        var d2Moves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("d2"));
        var d2Destinations = d2Moves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
        System.Diagnostics.Debug.WriteLine($"White pawn d2: [{string.Join(", ", d2Destinations)}]");
        Assert.Equal(new[] { "d3", "d4" }, d2Destinations);
        
        // White bishop on f1 should now have moves (knight moved)
        var f1Moves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("f1"));
        var f1Destinations = f1Moves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
        System.Diagnostics.Debug.WriteLine($"White bishop f1: [{string.Join(", ", f1Destinations)}]");
        Assert.Contains("e2", f1Destinations); // Should be able to move to e2
        
        // Black pawn on f7 should have 2 moves
        var f7Moves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("f7"));
        var f7Destinations = f7Moves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
        System.Diagnostics.Debug.WriteLine($"Black pawn f7: [{string.Join(", ", f7Destinations)}]");
        Assert.Equal(new[] { "f5", "f6" }, f7Destinations);
    }
    
    [Fact]
    public void TestKnightMoves()
    {
        // Test knight moves in various positions
        var gameState = new GameState();
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        // Move pawn to give knight more space
        arbiter.TryPlay(gameState, Square.FromAlgebraic("e2"), Square.FromAlgebraic("e4"));
        
        // White knight on g1 should have more moves now
        var g1Moves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("g1"));
        var g1Destinations = g1Moves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
        System.Diagnostics.Debug.WriteLine($"White knight g1: [{string.Join(", ", g1Destinations)}]");
        
        // Should be able to go to f3, h3, and possibly e2
        Assert.Contains("f3", g1Destinations);
        Assert.Contains("h3", g1Destinations);
        // e2 should NOT be available (blocked by our own pawn)
        Assert.DoesNotContain("e2", g1Destinations);
    }
    
    [Fact]
    public void TestRookMoves()
    {
        // Test rook moves when path is clear
        var gameState = new GameState();
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        // Clear path for rook by moving pawns
        arbiter.TryPlay(gameState, Square.FromAlgebraic("a2"), Square.FromAlgebraic("a4"));
        arbiter.TryPlay(gameState, Square.FromAlgebraic("a7"), Square.FromAlgebraic("a5"));
        
        // White rook on a1 should now have moves
        var a1Moves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("a1"));
        var a1Destinations = a1Moves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
        System.Diagnostics.Debug.WriteLine($"White rook a1: [{string.Join(", ", a1Destinations)}]");
        
        // Should be able to move up the a-file
        Assert.Contains("a2", a1Destinations);
        Assert.Contains("a3", a1Destinations);
        // Should NOT be able to move to a4 (occupied by our pawn)
        Assert.DoesNotContain("a4", a1Destinations);
    }
    
    [Fact]
    public void TestBishopMoves()
    {
        // Test bishop moves when path is clear
        var gameState = new GameState();
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        // Move pawn to free up bishop
        arbiter.TryPlay(gameState, Square.FromAlgebraic("e2"), Square.FromAlgebraic("e4"));
        
        // White bishop on f1 should now have moves
        var f1Moves = arbiter.LegalMovesFrom(gameState, Square.FromAlgebraic("f1"));
        var f1Destinations = f1Moves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
        System.Diagnostics.Debug.WriteLine($"White bishop f1: [{string.Join(", ", f1Destinations)}]");
        
        // Should be able to move diagonally
        Assert.Contains("e2", f1Destinations);
        Assert.Contains("d3", f1Destinations);
        Assert.Contains("c4", f1Destinations);
        Assert.Contains("b5", f1Destinations);
        Assert.Contains("a6", f1Destinations);
    }
}
