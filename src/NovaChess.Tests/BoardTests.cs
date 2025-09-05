using FluentAssertions;
using NovaChess.Core;
using Xunit;

namespace NovaChess.Tests;

public class BoardTests
{
    [Fact]
    public void NewBoard_ShouldHaveStartingPosition()
    {
        // Arrange & Act
        var board = new Board();
        
        // Assert
        board.SideToMove.Should().Be(PieceColor.White);
        board.CastleRights.Should().Be(CastleRights.All);
        board.EnPassantSquare.Should().Be(Square.None);
        board.HalfMoveClock.Should().Be(0);
        board.FullMoveNumber.Should().Be(1);
    }
    
    [Fact]
    public void StartingPosition_ShouldHaveCorrectPieces()
    {
        // Arrange
        var board = new Board();
        
        // Act & Assert
        // White pieces
        board.GetPiece(Square.FromAlgebraic("e1")).piece.Should().Be(PieceType.King);
        board.GetPiece(Square.FromAlgebraic("e1")).color.Should().Be(PieceColor.White);
        
        board.GetPiece(Square.FromAlgebraic("d1")).piece.Should().Be(PieceType.Queen);
        board.GetPiece(Square.FromAlgebraic("d1")).color.Should().Be(PieceColor.White);
        
        // White pawns
        board.GetPiece(Square.FromAlgebraic("e2")).piece.Should().Be(PieceType.Pawn);
        board.GetPiece(Square.FromAlgebraic("e2")).color.Should().Be(PieceColor.White);
        
        // Black pieces
        board.GetPiece(Square.FromAlgebraic("e8")).piece.Should().Be(PieceType.King);
        board.GetPiece(Square.FromAlgebraic("e8")).color.Should().Be(PieceColor.Black);
        
        board.GetPiece(Square.FromAlgebraic("d8")).piece.Should().Be(PieceType.Queen);
        board.GetPiece(Square.FromAlgebraic("d8")).color.Should().Be(PieceColor.Black);
        
        // Black pawns
        board.GetPiece(Square.FromAlgebraic("e7")).piece.Should().Be(PieceType.Pawn);
        board.GetPiece(Square.FromAlgebraic("e7")).color.Should().Be(PieceColor.Black);
    }
    
    [Fact]
    public void FEN_ShouldRoundTripCorrectly()
    {
        // Arrange
        var board = new Board();
        var originalFen = board.ToFen();
        
        // Act
        var newBoard = new Board(originalFen);
        var newFen = newBoard.ToFen();
        
        // Assert
        newFen.Should().Be(originalFen);
    }
    
    [Fact]
    public void SetPiece_ShouldUpdateBoardCorrectly()
    {
        // Arrange
        var board = new Board();
        var square = Square.FromAlgebraic("e4");
        
        // Act
        board.SetPiece(square, PieceType.Queen, PieceColor.White);
        
        // Assert
        var (piece, color) = board.GetPiece(square);
        piece.Should().Be(PieceType.Queen);
        color.Should().Be(PieceColor.White);
    }
    
    [Fact]
    public void IsSquareOccupied_ShouldReturnCorrectValue()
    {
        // Arrange
        var board = new Board();
        
        // Act & Assert
        board.IsSquareOccupied(Square.FromAlgebraic("e1")).Should().BeTrue();
        board.IsSquareOccupied(Square.FromAlgebraic("e3")).Should().BeFalse();
        board.IsSquareOccupied(Square.FromAlgebraic("e4")).Should().BeFalse();
    }
    
    [Fact]
    public void IsSquareOccupiedByColor_ShouldReturnCorrectValue()
    {
        // Arrange
        var board = new Board();
        
        // Act & Assert
        board.IsSquareOccupiedByColor(Square.FromAlgebraic("e1"), PieceColor.White).Should().BeTrue();
        board.IsSquareOccupiedByColor(Square.FromAlgebraic("e1"), PieceColor.Black).Should().BeFalse();
        board.IsSquareOccupiedByColor(Square.FromAlgebraic("e8"), PieceColor.Black).Should().BeTrue();
        board.IsSquareOccupiedByColor(Square.FromAlgebraic("e8"), PieceColor.White).Should().BeFalse();
    }
}
