using System.Collections.Generic;
using System.Linq;

namespace NovaChess.Core;

/// <summary>
/// Analyzes game state for checkmate, stalemate, draws, and other game-ending conditions
/// </summary>
public class GameAnalyzer
{
    private readonly IMoveGenerator _moveGenerator;
    
    public GameAnalyzer(IMoveGenerator moveGenerator)
    {
        _moveGenerator = moveGenerator;
    }
    
    /// <summary>
    /// Analyze the current game state and return the result
    /// </summary>
    public GameResult AnalyzePosition(GameState state)
    {
        // Check for draw conditions first (these apply regardless of legal moves)
        if (IsDrawByInsufficientMaterial(state))
        {
            return GameResult.Draw;
        }
        
        if (IsDrawByFiftyMoveRule(state))
        {
            return GameResult.Draw;
        }
        
        if (IsDrawByThreefoldRepetition(state))
        {
            return GameResult.Draw;
        }
        
        // Now check for checkmate/stalemate
        var legalMoves = _moveGenerator.GenerateLegalMoves(state).ToList();
        var isInCheck = _moveGenerator.IsInCheck(state, state.SideToMove);
        
        // No legal moves available
        if (legalMoves.Count == 0)
        {
            if (isInCheck)
            {
                // Checkmate - the side to move is in check and has no legal moves
                return state.SideToMove == Color.White ? GameResult.BlackWins : GameResult.WhiteWins;
            }
            else
            {
                // Stalemate - not in check but no legal moves
                return GameResult.Draw;
            }
        }
        
        // Game is still ongoing
        return GameResult.Ongoing;
    }
    
    /// <summary>
    /// Get the specific draw reason if the game is a draw
    /// </summary>
    public DrawReason GetDrawReason(GameState state)
    {
        // Check draw conditions in priority order
        if (IsDrawByInsufficientMaterial(state))
        {
            return DrawReason.InsufficientMaterial;
        }
        
        if (IsDrawByFiftyMoveRule(state))
        {
            return DrawReason.FiftyMoveRule;
        }
        
        if (IsDrawByThreefoldRepetition(state))
        {
            return DrawReason.ThreefoldRepetition;
        }
        
        // Check for stalemate (requires legal move generation)
        var legalMoves = _moveGenerator.GenerateLegalMoves(state).ToList();
        var isInCheck = _moveGenerator.IsInCheck(state, state.SideToMove);
        
        if (legalMoves.Count == 0 && !isInCheck)
        {
            return DrawReason.Stalemate;
        }
        
        return DrawReason.None;
    }
    
    /// <summary>
    /// Check if the current position is checkmate
    /// </summary>
    public bool IsCheckmate(GameState state)
    {
        var legalMoves = _moveGenerator.GenerateLegalMoves(state).ToList();
        var isInCheck = _moveGenerator.IsInCheck(state, state.SideToMove);
        
        return legalMoves.Count == 0 && isInCheck;
    }
    
    /// <summary>
    /// Check if the current position is stalemate
    /// </summary>
    public bool IsStalemate(GameState state)
    {
        var legalMoves = _moveGenerator.GenerateLegalMoves(state).ToList();
        var isInCheck = _moveGenerator.IsInCheck(state, state.SideToMove);
        
        return legalMoves.Count == 0 && !isInCheck;
    }
    
    /// <summary>
    /// Check for insufficient material draw
    /// </summary>
    private bool IsDrawByInsufficientMaterial(GameState state)
    {
        var whitePieces = new List<PieceType>();
        var blackPieces = new List<PieceType>();
        
        // Count all pieces on the board
        for (int i = 0; i < 64; i++)
        {
            var piece = state.GetPiece(new Square(i));
            if (!piece.IsEmpty && piece.Type != PieceType.King)
            {
                if (piece.IsWhite)
                    whitePieces.Add(piece.Type);
                else
                    blackPieces.Add(piece.Type);
            }
        }
        
        // King vs King
        if (whitePieces.Count == 0 && blackPieces.Count == 0)
            return true;
        
        // King vs King + Bishop
        if ((whitePieces.Count == 0 && blackPieces.Count == 1 && blackPieces[0] == PieceType.Bishop) ||
            (blackPieces.Count == 0 && whitePieces.Count == 1 && whitePieces[0] == PieceType.Bishop))
            return true;
        
        // King vs King + Knight
        if ((whitePieces.Count == 0 && blackPieces.Count == 1 && blackPieces[0] == PieceType.Knight) ||
            (blackPieces.Count == 0 && whitePieces.Count == 1 && whitePieces[0] == PieceType.Knight))
            return true;
        
        // King + Bishop vs King + Bishop (same color squares)
        if (whitePieces.Count == 1 && blackPieces.Count == 1 &&
            whitePieces[0] == PieceType.Bishop && blackPieces[0] == PieceType.Bishop)
        {
            // Check if bishops are on same color squares
            var whiteBishopSquare = FindPieceSquare(state, PieceType.Bishop, Color.White);
            var blackBishopSquare = FindPieceSquare(state, PieceType.Bishop, Color.Black);
            
            if (whiteBishopSquare.HasValue && blackBishopSquare.HasValue)
            {
                bool whiteBishopOnLight = (whiteBishopSquare.Value.File + whiteBishopSquare.Value.Rank) % 2 == 0;
                bool blackBishopOnLight = (blackBishopSquare.Value.File + blackBishopSquare.Value.Rank) % 2 == 0;
                
                if (whiteBishopOnLight == blackBishopOnLight)
                    return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check for 50-move rule draw
    /// </summary>
    private bool IsDrawByFiftyMoveRule(GameState state)
    {
        return state.HalfmoveClock >= 100; // 50 moves by each side = 100 half-moves
    }
    
    /// <summary>
    /// Check for threefold repetition draw
    /// </summary>
    private bool IsDrawByThreefoldRepetition(GameState state)
    {
        if (state.History.Count < 8) // Need at least 4 moves by each side for repetition
            return false;
        
        var currentHash = state.Zobrist;
        int repetitionCount = 1; // Current position counts as 1
        
        // Check previous positions in the game history
        foreach (var (move, delta) in state.History)
        {
            if (delta.OldZobrist == currentHash)
            {
                repetitionCount++;
                if (repetitionCount >= 3)
                    return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Find the square of a specific piece type and color
    /// </summary>
    private Square? FindPieceSquare(GameState state, PieceType pieceType, Color color)
    {
        for (int i = 0; i < 64; i++)
        {
            var square = new Square(i);
            var piece = state.GetPiece(square);
            if (piece.Type == pieceType && piece.Color == color)
            {
                return square;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get a human-readable description of the game result
    /// </summary>
    public string GetResultDescription(GameState state)
    {
        var result = AnalyzePosition(state);
        
        return result switch
        {
            GameResult.WhiteWins => IsCheckmate(state) ? "White wins by checkmate" : "White wins",
            GameResult.BlackWins => IsCheckmate(state) ? "Black wins by checkmate" : "Black wins",
            GameResult.Draw => GetDrawDescription(state),
            GameResult.Ongoing => GetOngoingDescription(state),
            _ => "Unknown result"
        };
    }
    
    private string GetDrawDescription(GameState state)
    {
        var reason = GetDrawReason(state);
        
        return reason switch
        {
            DrawReason.Stalemate => "Draw by stalemate",
            DrawReason.InsufficientMaterial => "Draw by insufficient material",
            DrawReason.FiftyMoveRule => "Draw by 50-move rule",
            DrawReason.ThreefoldRepetition => "Draw by threefold repetition",
            DrawReason.Agreement => "Draw by agreement",
            _ => "Draw"
        };
    }
    
    private string GetOngoingDescription(GameState state)
    {
        var isInCheck = _moveGenerator.IsInCheck(state, state.SideToMove);
        var sideToMove = state.SideToMove == Color.White ? "White" : "Black";
        
        if (isInCheck)
        {
            return $"{sideToMove} is in check";
        }
        
        return $"{sideToMove} to move";
    }
}
