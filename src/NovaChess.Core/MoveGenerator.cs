namespace NovaChess.Core;

public class MoveGenerator
{
    private readonly Board _board;
    
    public MoveGenerator(Board board)
    {
        _board = board;
    }
    
    public List<Move> GenerateLegalMoves()
    {
        var moves = new List<Move>();
        var pseudoLegalMoves = GeneratePseudoLegalMoves();
        
        foreach (var move in pseudoLegalMoves)
        {
            if (IsMoveLegal(move))
            {
                moves.Add(move);
            }
        }
        
        return moves;
    }
    
    public List<Move> GeneratePseudoLegalMoves()
    {
        var moves = new List<Move>();
        
        for (int i = 0; i < 64; i++)
        {
            var square = new Square(i);
            var (piece, color) = _board.GetPiece(square);
            
            if (piece != PieceType.None && color == _board.SideToMove)
            {
                switch (piece)
                {
                    case PieceType.Pawn:
                        moves.AddRange(GeneratePawnMoves(square, color));
                        break;
                    case PieceType.Knight:
                        moves.AddRange(GenerateKnightMoves(square, color));
                        break;
                    case PieceType.Bishop:
                        moves.AddRange(GenerateBishopMoves(square, color));
                        break;
                    case PieceType.Rook:
                        moves.AddRange(GenerateRookMoves(square, color));
                        break;
                    case PieceType.Queen:
                        moves.AddRange(GenerateQueenMoves(square, color));
                        break;
                    case PieceType.King:
                        moves.AddRange(GenerateKingMoves(square, color));
                        break;
                }
            }
        }
        
        return moves;
    }
    
    private List<Move> GeneratePawnMoves(Square square, PieceColor color)
    {
        var moves = new List<Move>();
        int direction = color == PieceColor.White ? 1 : -1;
        int startRank = color == PieceColor.White ? 1 : 6;
        int promotionRank = color == PieceColor.White ? 7 : 0;
        
        // Single push
        var singlePush = new Square(square.File, square.Rank + direction);
        if (singlePush.IsValid && !_board.IsSquareOccupied(singlePush))
        {
            if (singlePush.Rank == promotionRank)
            {
                // Promotion moves
                moves.Add(new Move(square, singlePush, PieceType.Pawn, PieceType.Queen));
                moves.Add(new Move(square, singlePush, PieceType.Pawn, PieceType.Rook));
                moves.Add(new Move(square, singlePush, PieceType.Pawn, PieceType.Bishop));
                moves.Add(new Move(square, singlePush, PieceType.Pawn, PieceType.Knight));
            }
            else
            {
                moves.Add(new Move(square, singlePush, PieceType.Pawn));
            }
            
            // Double push from starting rank
            if (square.Rank == startRank)
            {
                var doublePush = new Square(square.File, square.Rank + 2 * direction);
                if (!_board.IsSquareOccupied(doublePush))
                {
                    moves.Add(new Move(square, doublePush, PieceType.Pawn));
                }
            }
        }
        
        // Captures
        var captureSquares = new[]
        {
            new Square(square.File - 1, square.Rank + direction),
            new Square(square.File + 1, square.Rank + direction)
        };
        
        foreach (var captureSquare in captureSquares)
        {
            if (captureSquare.IsValid && 
                _board.IsSquareOccupiedByColor(captureSquare, color == PieceColor.White ? PieceColor.Black : PieceColor.White))
            {
                if (captureSquare.Rank == promotionRank)
                {
                    // Promotion captures
                    moves.Add(new Move(square, captureSquare, PieceType.Pawn, PieceType.Queen));
                    moves.Add(new Move(square, captureSquare, PieceType.Pawn, PieceType.Rook));
                    moves.Add(new Move(square, captureSquare, PieceType.Pawn, PieceType.Bishop));
                    moves.Add(new Move(square, captureSquare, PieceType.Pawn, PieceType.Knight));
                }
                else
                {
                    moves.Add(new Move(square, captureSquare, PieceType.Pawn));
                }
            }
        }
        
        // En passant
        if (_board.EnPassantSquare.IsValid && 
            Math.Abs(square.File - _board.EnPassantSquare.File) == 1 &&
            square.Rank == (color == PieceColor.White ? 4 : 3))
        {
            moves.Add(new Move(square, _board.EnPassantSquare, PieceType.Pawn));
        }
        
        return moves;
    }
    
    private List<Move> GenerateKnightMoves(Square square, PieceColor color)
    {
        var moves = new List<Move>();
        var knightOffsets = new[]
        {
            (-2, -1), (-2, 1), (-1, -2), (-1, 2),
            (1, -2), (1, 2), (2, -1), (2, 1)
        };
        
        foreach (var (fileOffset, rankOffset) in knightOffsets)
        {
            var targetSquare = new Square(square.File + fileOffset, square.Rank + rankOffset);
            if (targetSquare.IsValid && !_board.IsSquareOccupiedByColor(targetSquare, color))
            {
                moves.Add(new Move(square, targetSquare, PieceType.Knight));
            }
        }
        
        return moves;
    }
    
    private List<Move> GenerateBishopMoves(Square square, PieceColor color)
    {
        return GenerateSlidingMoves(square, color, PieceType.Bishop, new[] { (-1, -1), (-1, 1), (1, -1), (1, 1) });
    }
    
    private List<Move> GenerateRookMoves(Square square, PieceColor color)
    {
        return GenerateSlidingMoves(square, color, PieceType.Rook, new[] { (-1, 0), (1, 0), (0, -1), (0, 1) });
    }
    
    private List<Move> GenerateQueenMoves(Square square, PieceColor color)
    {
        var moves = new List<Move>();
        moves.AddRange(GenerateBishopMoves(square, color));
        moves.AddRange(GenerateRookMoves(square, color));
        return moves;
    }
    
    private List<Move> GenerateKingMoves(Square square, PieceColor color)
    {
        var moves = new List<Move>();
        
        // Normal king moves
        for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
        {
            for (int rankOffset = -1; rankOffset <= 1; rankOffset++)
            {
                if (fileOffset == 0 && rankOffset == 0) continue;
                
                var targetSquare = new Square(square.File + fileOffset, square.Rank + rankOffset);
                if (targetSquare.IsValid && !_board.IsSquareOccupiedByColor(targetSquare, color))
                {
                    moves.Add(new Move(square, targetSquare, PieceType.King));
                }
            }
        }
        
        // Castling
        if (color == PieceColor.White)
        {
            if ((_board.CastleRights & CastleRights.WhiteKingside) != 0)
            {
                if (!_board.IsSquareOccupied(new Square(5, 0)) && 
                    !_board.IsSquareOccupied(new Square(6, 0)) &&
                    !IsSquareUnderAttack(new Square(5, 0), PieceColor.Black) &&
                    !IsSquareUnderAttack(new Square(6, 0), PieceColor.Black))
                {
                    moves.Add(new Move(square, new Square(6, 0), PieceType.King));
                }
            }
            
            if ((_board.CastleRights & CastleRights.WhiteQueenside) != 0)
            {
                if (!_board.IsSquareOccupied(new Square(3, 0)) && 
                    !_board.IsSquareOccupied(new Square(2, 0)) &&
                    !IsSquareUnderAttack(new Square(3, 0), PieceColor.Black) &&
                    !IsSquareUnderAttack(new Square(2, 0), PieceColor.Black))
                {
                    moves.Add(new Move(square, new Square(2, 0), PieceType.King));
                }
            }
        }
        else
        {
            if ((_board.CastleRights & CastleRights.BlackKingside) != 0)
            {
                if (!_board.IsSquareOccupied(new Square(5, 7)) && 
                    !_board.IsSquareOccupied(new Square(6, 7)) &&
                    !IsSquareUnderAttack(new Square(5, 7), PieceColor.White) &&
                    !IsSquareUnderAttack(new Square(6, 7), PieceColor.White))
                {
                    moves.Add(new Move(square, new Square(6, 7), PieceType.King));
                }
            }
            
            if ((_board.CastleRights & CastleRights.BlackQueenside) != 0)
            {
                if (!_board.IsSquareOccupied(new Square(3, 7)) && 
                    !_board.IsSquareOccupied(new Square(2, 7)) &&
                    !IsSquareUnderAttack(new Square(3, 7), PieceColor.White) &&
                    !IsSquareUnderAttack(new Square(2, 7), PieceColor.White))
                {
                    moves.Add(new Move(square, new Square(2, 7), PieceType.King));
                }
            }
        }
        
        return moves;
    }
    
    private List<Move> GenerateSlidingMoves(Square square, PieceColor color, PieceType piece, (int, int)[] directions)
    {
        var moves = new List<Move>();
        
        foreach (var (fileOffset, rankOffset) in directions)
        {
            int file = square.File + fileOffset;
            int rank = square.Rank + rankOffset;
            
            while (file >= 0 && file < 8 && rank >= 0 && rank < 8)
            {
                var targetSquare = new Square(file, rank);
                var (targetPiece, targetColor) = _board.GetPiece(targetSquare);
                
                if (targetPiece == PieceType.None)
                {
                    moves.Add(new Move(square, targetSquare, piece));
                }
                else
                {
                    if (targetColor != color)
                    {
                        moves.Add(new Move(square, targetSquare, piece));
                    }
                    break;
                }
                
                file += fileOffset;
                rank += rankOffset;
            }
        }
        
        return moves;
    }
    
    private bool IsSquareUnderAttack(Square square, PieceColor byColor)
    {
        // Check if any piece of the given color can attack this square
        // This is a simplified implementation - in production you'd want to optimize this
        
        for (int i = 0; i < 64; i++)
        {
            var attackerSquare = new Square(i);
            var (piece, color) = _board.GetPiece(attackerSquare);
            
            if (piece != PieceType.None && color == byColor)
            {
                var pseudoLegalMoves = piece switch
                {
                    PieceType.Pawn => GeneratePawnMoves(attackerSquare, color),
                    PieceType.Knight => GenerateKnightMoves(attackerSquare, color),
                    PieceType.Bishop => GenerateBishopMoves(attackerSquare, color),
                    PieceType.Rook => GenerateRookMoves(attackerSquare, color),
                    PieceType.Queen => GenerateQueenMoves(attackerSquare, color),
                    PieceType.King => GenerateKingMoves(attackerSquare, color),
                    _ => new List<Move>()
                };
                
                if (pseudoLegalMoves.Any(m => m.To == square))
                    return true;
            }
        }
        
        return false;
    }
    
    private bool IsMoveLegal(Move move)
    {
        // Make the move temporarily and check if the king is in check
        // This is a simplified implementation - in production you'd want to optimize this
        
        // For now, we'll assume all pseudo-legal moves are legal
        // In a full implementation, you'd need to check if the move leaves the king in check
        return true;
    }
}
