using Xunit;
using NovaChess.Core;
using CoreColor = NovaChess.Core.Color;

namespace NovaChess.Tests;

public class CoreEngineTests
{
    [Fact]
    public void GameState_InitialPosition_IsCorrect()
    {
        // Arrange & Act
        var gameState = new GameState();
        
        // Assert
        Assert.Equal(CoreColor.White, gameState.SideToMove);
        Assert.Equal(PieceType.Rook, gameState.GetPiece(new Square(0, 0)).Type);
        Assert.Equal(CoreColor.White, gameState.GetPiece(new Square(0, 0)).Color);
        Assert.Equal(PieceType.King, gameState.GetPiece(new Square(4, 0)).Type);
        Assert.Equal(PieceType.King, gameState.GetPiece(new Square(4, 7)).Type);
        Assert.Equal(CoreColor.Black, gameState.GetPiece(new Square(4, 7)).Color);
    }
    
    [Fact]
    public void Arbiter_WhiteCannotMoveWhenBlackToMove()
    {
        // Arrange
        var gameState = new GameState();
        gameState.SideToMove = CoreColor.Black;
        var arbiter = new Arbiter();
        
        // Try to move white pawn
        var move = new Move 
        { 
            From = new Square(4, 1), 
            To = new Square(4, 2),
            Kind = MoveKind.Quiet,
            MovingPiece = PieceType.Pawn,
            MovingColor = CoreColor.White
        };
        
        // Act & Assert
        Assert.False(arbiter.TryPlay(gameState, move));
        Assert.Equal(CoreColor.Black, gameState.SideToMove); // Turn should not change
    }
    
    [Fact]
    public void MoveGenerator_InitialPosition_HasCorrectPawnMoves()
    {
        // Arrange
        var gameState = new GameState();
        var moveGenerator = new ChessMoveGenerator();
        
        // Act
        var moves = moveGenerator.GenerateLegalMovesFrom(gameState, new Square(4, 1)); // e2 pawn
        
        // Assert
        Assert.Equal(2, moves.Count()); // Should have 2 moves: e3 and e4
        Assert.Contains(moves, m => m.To == new Square(4, 2)); // e3
        Assert.Contains(moves, m => m.To == new Square(4, 3)); // e4
    }
    
    [Fact]
    public void MoveGenerator_InitialPosition_HasCorrectKnightMoves()
    {
        // Arrange
        var gameState = new GameState();
        var moveGenerator = new ChessMoveGenerator();
        
        // Act
        var moves = moveGenerator.GenerateLegalMovesFrom(gameState, new Square(1, 0)); // b1 knight
        
        // Assert
        Assert.Equal(2, moves.Count()); // Should have 2 moves: a3 and c3
        Assert.Contains(moves, m => m.To == new Square(0, 2)); // a3
        Assert.Contains(moves, m => m.To == new Square(2, 2)); // c3
    }
    
    [Fact]
    public void Arbiter_ValidPawnMove_Succeeds()
    {
        // Arrange
        var gameState = new GameState();
        var arbiter = new Arbiter();
        
        // Act
        var move = arbiter.TryPlay(gameState, new Square(4, 1), new Square(4, 2));
        
        // Assert
        Assert.NotNull(move);
        Assert.Equal(CoreColor.Black, gameState.SideToMove); // Turn should switch
        Assert.Equal(PieceType.Pawn, gameState.GetPiece(new Square(4, 2)).Type);
        Assert.True(gameState.GetPiece(new Square(4, 1)).IsEmpty);
    }
    
    [Fact]
    public void Arbiter_InvalidMove_Fails()
    {
        // Arrange
        var gameState = new GameState();
        var arbiter = new Arbiter();
        
        // Act - try to move pawn 3 squares forward (invalid)
        var move = arbiter.TryPlay(gameState, new Square(4, 1), new Square(4, 4));
        
        // Assert
        Assert.Null(move);
        Assert.Equal(CoreColor.White, gameState.SideToMove); // Turn should not change
        Assert.Equal(PieceType.Pawn, gameState.GetPiece(new Square(4, 1)).Type); // Pawn should still be there
    }
    
    [Fact]
    public void Square_AlgebraicNotation_WorksCorrectly()
    {
        // Arrange & Act
        var e4 = Square.FromAlgebraic("e4");
        var a1 = Square.FromAlgebraic("a1");
        var h8 = Square.FromAlgebraic("h8");
        
        // Assert
        Assert.Equal(4, e4.File);
        Assert.Equal(3, e4.Rank);
        Assert.Equal("e4", e4.ToAlgebraic());
        
        Assert.Equal(0, a1.File);
        Assert.Equal(0, a1.Rank);
        Assert.Equal("a1", a1.ToAlgebraic());
        
        Assert.Equal(7, h8.File);
        Assert.Equal(7, h8.Rank);
        Assert.Equal("h8", h8.ToAlgebraic());
    }
}
