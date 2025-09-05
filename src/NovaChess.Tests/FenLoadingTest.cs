using Xunit;
using NovaChess.Core;
using CoreColor = NovaChess.Core.Color;

namespace NovaChess.Tests;

/// <summary>
/// Test FEN loading functionality
/// </summary>
public class FenLoadingTest
{
    [Fact]
    public void LoadStartingPosition_CorrectPieces()
    {
        var gameState = new GameState();
        var startingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        
        gameState.LoadFromFen(startingFen);
        
        // Check a few key pieces
        var whiteKing = gameState.GetPiece(Square.FromAlgebraic("e1"));
        var blackKing = gameState.GetPiece(Square.FromAlgebraic("e8"));
        var whitePawn = gameState.GetPiece(Square.FromAlgebraic("e2"));
        var blackPawn = gameState.GetPiece(Square.FromAlgebraic("e7"));
        
        Assert.Equal(PieceType.King, whiteKing.Type);
        Assert.Equal(CoreColor.White, whiteKing.Color);
        Assert.Equal(PieceType.King, blackKing.Type);
        Assert.Equal(CoreColor.Black, blackKing.Color);
        Assert.Equal(PieceType.Pawn, whitePawn.Type);
        Assert.Equal(CoreColor.White, whitePawn.Color);
        Assert.Equal(PieceType.Pawn, blackPawn.Type);
        Assert.Equal(CoreColor.Black, blackPawn.Color);
        
        Assert.Equal(CoreColor.White, gameState.SideToMove);
    }
    
    [Fact]
    public void LoadSimplePosition_CorrectAnalysis()
    {
        var gameState = new GameState();
        var arbiter = new Arbiter(new ChessMoveGenerator());
        
        // King vs King position
        gameState.LoadFromFen("8/8/8/8/8/8/4K3/4k3 w - - 0 1");
        
        System.Diagnostics.Debug.WriteLine($"Loaded position: {gameState.ToFen()}");
        
        // Check pieces are loaded correctly
        var whiteKing = gameState.GetPiece(Square.FromAlgebraic("e2"));
        var blackKing = gameState.GetPiece(Square.FromAlgebraic("e1"));
        
        System.Diagnostics.Debug.WriteLine($"White King at e2: {whiteKing}");
        System.Diagnostics.Debug.WriteLine($"Black King at e1: {blackKing}");
        
        Assert.Equal(PieceType.King, whiteKing.Type);
        Assert.Equal(CoreColor.White, whiteKing.Color);
        Assert.Equal(PieceType.King, blackKing.Type);
        Assert.Equal(CoreColor.Black, blackKing.Color);
        
        var legalMoves = arbiter.LegalMoves(gameState);
        System.Diagnostics.Debug.WriteLine($"Legal moves: {legalMoves.Count}");
        
        var result = arbiter.AnalyzePosition(gameState);
        System.Diagnostics.Debug.WriteLine($"Game result: {result}");
        
        // This should be a draw by insufficient material
        Assert.Equal(GameResult.Draw, result);
    }
}
