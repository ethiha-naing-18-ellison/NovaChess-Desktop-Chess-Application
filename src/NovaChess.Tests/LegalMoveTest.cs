using NovaChess.Core;
using Xunit;
using System.Linq;

namespace NovaChess.Tests;

public class LegalMoveTest
{
    [Fact]
    public void WhitePawn_StartingPosition_ShouldHaveTwoMoves()
    {
        // Arrange
        var gameState = new GameState(); // Starting position
        var arbiter = new Arbiter(new ChessMoveGenerator());
        var e2Square = Square.FromAlgebraic("e2"); // White pawn on e2
        
        // Act
        var legalMoves = arbiter.LegalMovesFrom(gameState, e2Square);
        
        // Assert
        Assert.Equal(2, legalMoves.Count); // Should be able to move to e3 and e4
        
        var destinations = legalMoves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToList();
        Assert.Contains("e3", destinations);
        Assert.Contains("e4", destinations);
        
        // Debug output
        System.Diagnostics.Debug.WriteLine($"White pawn on e2 can move to: {string.Join(", ", destinations)}");
    }
    
    [Fact]
    public void WhiteKnight_StartingPosition_ShouldHaveCorrectMoves()
    {
        // Arrange
        var gameState = new GameState(); // Starting position
        var arbiter = new Arbiter(new ChessMoveGenerator());
        var b1Square = Square.FromAlgebraic("b1"); // White knight on b1
        
        // Act
        var legalMoves = arbiter.LegalMovesFrom(gameState, b1Square);
        
        // Assert
        Assert.Equal(2, legalMoves.Count); // Should be able to move to a3 and c3
        
        var destinations = legalMoves.Select(m => m.To.ToAlgebraic()).OrderBy(s => s).ToList();
        Assert.Contains("a3", destinations);
        Assert.Contains("c3", destinations);
        
        // Debug output
        System.Diagnostics.Debug.WriteLine($"White knight on b1 can move to: {string.Join(", ", destinations)}");
    }
}
