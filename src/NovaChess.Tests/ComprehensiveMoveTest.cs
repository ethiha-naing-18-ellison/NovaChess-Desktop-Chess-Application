using NovaChess.Core;
using Xunit;
using System.Linq;

namespace NovaChess.Tests;

public class ComprehensiveMoveTest
{
    [Fact]
    public void StartingPosition_AllWhitePieces_ShouldHaveCorrectMoves()
    {
        // Arrange
        var gameState = new GameState(); // Starting position
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        // Test all white pieces in starting position
        var testCases = new[]
        {
            // White pawns - should have 2 moves each (one forward, two forward)
            new { Square = "a2", ExpectedMoves = new[] { "a3", "a4" } },
            new { Square = "b2", ExpectedMoves = new[] { "b3", "b4" } },
            new { Square = "c2", ExpectedMoves = new[] { "c3", "c4" } },
            new { Square = "d2", ExpectedMoves = new[] { "d3", "d4" } },
            new { Square = "e2", ExpectedMoves = new[] { "e3", "e4" } },
            new { Square = "f2", ExpectedMoves = new[] { "f3", "f4" } },
            new { Square = "g2", ExpectedMoves = new[] { "g3", "g4" } },
            new { Square = "h2", ExpectedMoves = new[] { "h3", "h4" } },
            
            // White knights - should have 2 moves each
            new { Square = "b1", ExpectedMoves = new[] { "a3", "c3" } },
            new { Square = "g1", ExpectedMoves = new[] { "f3", "h3" } },
            
            // Other pieces should have 0 moves (blocked by pawns)
            new { Square = "a1", ExpectedMoves = new string[0] }, // Rook blocked
            new { Square = "c1", ExpectedMoves = new string[0] }, // Bishop blocked
            new { Square = "d1", ExpectedMoves = new string[0] }, // Queen blocked
            new { Square = "e1", ExpectedMoves = new string[0] }, // King blocked
            new { Square = "f1", ExpectedMoves = new string[0] }, // Bishop blocked
            new { Square = "h1", ExpectedMoves = new string[0] }, // Rook blocked
        };
        
        foreach (var testCase in testCases)
        {
            // Act
            var square = Square.FromAlgebraic(testCase.Square);
            var legalMoves = arbiter.LegalMovesFrom(gameState, square);
            var actualMoves = legalMoves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
            var expectedMoves = testCase.ExpectedMoves.OrderBy(s => s).ToArray();
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"{testCase.Square}: Expected [{string.Join(", ", expectedMoves)}], Got [{string.Join(", ", actualMoves)}]");
            
            // Assert
            Assert.Equal(expectedMoves, actualMoves);
        }
    }
    
    [Fact]
    public void TestSpecificProblemPieces()
    {
        // Test pieces that are commonly problematic
        var gameState = new GameState();
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        // Test white pawn on e2 specifically
        var e2 = Square.FromAlgebraic("e2");
        var e2Moves = arbiter.LegalMovesFrom(gameState, e2);
        var e2Destinations = e2Moves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
        
        System.Diagnostics.Debug.WriteLine($"🔍 WHITE PAWN e2 moves: [{string.Join(", ", e2Destinations)}]");
        Assert.Equal(new[] { "e3", "e4" }, e2Destinations);
        
        // Test white knight on b1 specifically  
        var b1 = Square.FromAlgebraic("b1");
        var b1Moves = arbiter.LegalMovesFrom(gameState, b1);
        var b1Destinations = b1Moves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
        
        System.Diagnostics.Debug.WriteLine($"🔍 WHITE KNIGHT b1 moves: [{string.Join(", ", b1Destinations)}]");
        Assert.Equal(new[] { "a3", "c3" }, b1Destinations);
        
        // Test white king on e1 (should have no moves)
        var e1 = Square.FromAlgebraic("e1");
        var e1Moves = arbiter.LegalMovesFrom(gameState, e1);
        var e1Destinations = e1Moves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToArray();
        
        System.Diagnostics.Debug.WriteLine($"🔍 WHITE KING e1 moves: [{string.Join(", ", e1Destinations)}]");
        Assert.Empty(e1Destinations);
    }
}
