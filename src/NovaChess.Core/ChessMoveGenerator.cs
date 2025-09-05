using System.Collections.Generic;

namespace NovaChess.Core;

public sealed class ChessMoveGenerator : IMoveGenerator
{
    private static readonly int[] KnightOffsets = { -17, -15, -10, -6, 6, 10, 15, 17 };
    private static readonly int[] KingOffsets = { -9, -8, -7, -1, 1, 7, 8, 9 };
    private static readonly int[] DiagonalOffsets = { -9, -7, 7, 9 };
    private static readonly int[] StraightOffsets = { -8, -1, 1, 8 };
    
    public IEnumerable<Move> GeneratePseudoLegalMoves(GameState state)
    {
        var moves = new List<Move>();
        var sideToMove = state.SideToMove;
        
        for (int i = 0; i < 64; i++)
        {
            var square = new Square(i);
            var piece = state.GetPiece(square);
            
            if (piece.IsEmpty || piece.Color != sideToMove)
                continue;
                
            moves.AddRange(GeneratePieceMoves(state, square, piece));
        }
        
        return moves;
    }
    
    public IEnumerable<Move> GenerateLegalMoves(GameState state)
    {
        var pseudoMoves = GeneratePseudoLegalMoves(state);
        var legalMoves = new List<Move>();
        
        foreach (var move in pseudoMoves)
        {
            var delta = ApplyMove(state, move);
            
            // Check if the move leaves our king in check
            if (!IsInCheck(state, move.MovingColor))
            {
                legalMoves.Add(move);
            }
            
            UnapplyMove(state, move, delta);
        }
        
        return legalMoves;
    }
    
    public IEnumerable<Move> GenerateLegalMovesFrom(GameState state, Square from)
    {
        var piece = state.GetPiece(from);
        if (piece.IsEmpty || piece.Color != state.SideToMove)
            return Enumerable.Empty<Move>();
            
        var pseudoMoves = GeneratePieceMoves(state, from, piece);
        var legalMoves = new List<Move>();
        
        foreach (var move in pseudoMoves)
        {
            var delta = ApplyMove(state, move);
            
            if (!IsInCheck(state, move.MovingColor))
            {
                legalMoves.Add(move);
            }
            
            UnapplyMove(state, move, delta);
        }
        
        return legalMoves;
    }
    
    public bool IsInCheck(GameState state, Color color)
    {
        var kingSquare = state.FindKing(color);
        if (kingSquare == Square.None) return false;
        
        return SquareIsAttacked(state, kingSquare, color == Color.White ? Color.Black : Color.White);
    }
    
    public bool SquareIsAttacked(GameState state, Square square, Color byColor)
    {
        // Check pawn attacks
        var pawnDirection = byColor == Color.White ? 1 : -1;
        var pawnRank = square.Rank - pawnDirection;
        
        if (pawnRank >= 0 && pawnRank <= 7)
        {
            if (square.File > 0)
            {
                var leftPawn = new Square(square.File - 1, pawnRank);
                var piece = state.GetPiece(leftPawn);
                if (piece.Type == PieceType.Pawn && piece.Color == byColor)
                    return true;
            }
            
            if (square.File < 7)
            {
                var rightPawn = new Square(square.File + 1, pawnRank);
                var piece = state.GetPiece(rightPawn);
                if (piece.Type == PieceType.Pawn && piece.Color == byColor)
                    return true;
            }
        }
        
        // Check knight attacks
        foreach (var offset in KnightOffsets)
        {
            var targetIndex = square.Index + offset;
            if (targetIndex >= 0 && targetIndex < 64)
            {
                var target = new Square(targetIndex);
                if (Math.Abs(target.File - square.File) <= 2 && Math.Abs(target.Rank - square.Rank) <= 2)
                {
                    var piece = state.GetPiece(target);
                    if (piece.Type == PieceType.Knight && piece.Color == byColor)
                        return true;
                }
            }
        }
        
        // Check king attacks
        foreach (var offset in KingOffsets)
        {
            var targetIndex = square.Index + offset;
            if (targetIndex >= 0 && targetIndex < 64)
            {
                var target = new Square(targetIndex);
                if (Math.Abs(target.File - square.File) <= 1 && Math.Abs(target.Rank - square.Rank) <= 1)
                {
                    var piece = state.GetPiece(target);
                    if (piece.Type == PieceType.King && piece.Color == byColor)
                        return true;
                }
            }
        }
        
        // Check sliding piece attacks (rook, bishop, queen)
        if (CheckSlidingAttacks(state, square, byColor, StraightOffsets, PieceType.Rook, PieceType.Queen) ||
            CheckSlidingAttacks(state, square, byColor, DiagonalOffsets, PieceType.Bishop, PieceType.Queen))
        {
            return true;
        }
        
        return false;
    }
    
    private bool CheckSlidingAttacks(GameState state, Square square, Color byColor, int[] offsets, 
        PieceType pieceType1, PieceType pieceType2)
    {
        foreach (var offset in offsets)
        {
            for (int distance = 1; distance < 8; distance++)
            {
                var targetIndex = square.Index + offset * distance;
                if (targetIndex < 0 || targetIndex >= 64) break;
                
                var target = new Square(targetIndex);
                
                // Check if we've wrapped around the board
                if (Math.Abs(target.File - square.File) > distance || Math.Abs(target.Rank - square.Rank) > distance)
                    break;
                
                var piece = state.GetPiece(target);
                if (!piece.IsEmpty)
                {
                    if (piece.Color == byColor && (piece.Type == pieceType1 || piece.Type == pieceType2))
                        return true;
                    break; // Piece blocks further movement
                }
            }
        }
        return false;
    }
    
    private IEnumerable<Move> GeneratePieceMoves(GameState state, Square from, Piece piece)
    {
        return piece.Type switch
        {
            PieceType.Pawn => GeneratePawnMoves(state, from, piece.Color),
            PieceType.Knight => GenerateKnightMoves(state, from, piece.Color),
            PieceType.Bishop => GenerateBishopMoves(state, from, piece.Color),
            PieceType.Rook => GenerateRookMoves(state, from, piece.Color),
            PieceType.Queen => GenerateQueenMoves(state, from, piece.Color),
            PieceType.King => GenerateKingMoves(state, from, piece.Color),
            _ => Enumerable.Empty<Move>()
        };
    }
    
    private IEnumerable<Move> GeneratePawnMoves(GameState state, Square from, Color color)
    {
        var moves = new List<Move>();
        var direction = color == Color.White ? 1 : -1;
        var startRank = color == Color.White ? 1 : 6;
        var promotionRank = color == Color.White ? 7 : 0;
        
        // Forward moves
        var oneForward = new Square(from.File, from.Rank + direction);
        if (oneForward.IsValid && state.GetPiece(oneForward).IsEmpty)
        {
            if (oneForward.Rank == promotionRank)
            {
                // Promotion moves
                moves.Add(CreateMove(from, oneForward, MoveKind.Promotion, PieceType.Pawn, color, PieceType.Queen));
                moves.Add(CreateMove(from, oneForward, MoveKind.Promotion, PieceType.Pawn, color, PieceType.Rook));
                moves.Add(CreateMove(from, oneForward, MoveKind.Promotion, PieceType.Pawn, color, PieceType.Bishop));
                moves.Add(CreateMove(from, oneForward, MoveKind.Promotion, PieceType.Pawn, color, PieceType.Knight));
            }
            else
            {
                moves.Add(CreateMove(from, oneForward, MoveKind.Quiet, PieceType.Pawn, color));
                
                // Double push from starting position
                if (from.Rank == startRank)
                {
                    var twoForward = new Square(from.File, from.Rank + 2 * direction);
                    if (twoForward.IsValid && state.GetPiece(twoForward).IsEmpty)
                    {
                        moves.Add(CreateMove(from, twoForward, MoveKind.Quiet, PieceType.Pawn, color));
                    }
                }
            }
        }
        
        // Captures
        for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
        {
            int newFile = from.File + fileOffset;
            int newRank = from.Rank + direction;
            
            // Check bounds before creating Square
            if (newFile < 0 || newFile > 7 || newRank < 0 || newRank > 7) continue;
            
            var captureSquare = new Square(newFile, newRank);
            
            var targetPiece = state.GetPiece(captureSquare);
            
            // Regular capture
            if (!targetPiece.IsEmpty && targetPiece.Color != color)
            {
                if (captureSquare.Rank == promotionRank)
                {
                    // Capture with promotion
                    moves.Add(CreateMove(from, captureSquare, MoveKind.Promotion, PieceType.Pawn, color, PieceType.Queen, targetPiece.Type));
                    moves.Add(CreateMove(from, captureSquare, MoveKind.Promotion, PieceType.Pawn, color, PieceType.Rook, targetPiece.Type));
                    moves.Add(CreateMove(from, captureSquare, MoveKind.Promotion, PieceType.Pawn, color, PieceType.Bishop, targetPiece.Type));
                    moves.Add(CreateMove(from, captureSquare, MoveKind.Promotion, PieceType.Pawn, color, PieceType.Knight, targetPiece.Type));
                }
                else
                {
                    moves.Add(CreateMove(from, captureSquare, MoveKind.Capture, PieceType.Pawn, color, PieceType.None, targetPiece.Type));
                }
            }
            // En passant
            else if (state.EnPassantFile.HasValue && captureSquare.File == state.EnPassantFile.Value && 
                     captureSquare.Rank == (color == Color.White ? 5 : 2))
            {
                moves.Add(CreateMove(from, captureSquare, MoveKind.EnPassant, PieceType.Pawn, color));
            }
        }
        
        return moves;
    }
    
    private IEnumerable<Move> GenerateKnightMoves(GameState state, Square from, Color color)
    {
        var moves = new List<Move>();
        
        foreach (var offset in KnightOffsets)
        {
            var targetIndex = from.Index + offset;
            if (targetIndex >= 0 && targetIndex < 64)
            {
                var target = new Square(targetIndex);
                
                // Check for valid knight move (L-shape)
                var fileDiff = Math.Abs(target.File - from.File);
                var rankDiff = Math.Abs(target.Rank - from.Rank);
                
                if ((fileDiff == 2 && rankDiff == 1) || (fileDiff == 1 && rankDiff == 2))
                {
                    var targetPiece = state.GetPiece(target);
                    if (targetPiece.IsEmpty)
                    {
                        moves.Add(CreateMove(from, target, MoveKind.Quiet, PieceType.Knight, color));
                    }
                    else if (targetPiece.Color != color)
                    {
                        moves.Add(CreateMove(from, target, MoveKind.Capture, PieceType.Knight, color, PieceType.None, targetPiece.Type));
                    }
                }
            }
        }
        
        return moves;
    }
    
    private IEnumerable<Move> GenerateSlidingMoves(GameState state, Square from, Color color, 
        PieceType pieceType, int[] offsets)
    {
        var moves = new List<Move>();
        
        foreach (var offset in offsets)
        {
            for (int distance = 1; distance < 8; distance++)
            {
                var targetIndex = from.Index + offset * distance;
                if (targetIndex < 0 || targetIndex >= 64) break;
                
                var target = new Square(targetIndex);
                
                // Check for board wrap-around
                if (Math.Abs(target.File - from.File) > distance || Math.Abs(target.Rank - from.Rank) > distance)
                    break;
                
                var targetPiece = state.GetPiece(target);
                if (targetPiece.IsEmpty)
                {
                    moves.Add(CreateMove(from, target, MoveKind.Quiet, pieceType, color));
                }
                else
                {
                    if (targetPiece.Color != color)
                    {
                        moves.Add(CreateMove(from, target, MoveKind.Capture, pieceType, color, PieceType.None, targetPiece.Type));
                    }
                    break; // Piece blocks further movement
                }
            }
        }
        
        return moves;
    }
    
    private IEnumerable<Move> GenerateRookMoves(GameState state, Square from, Color color) =>
        GenerateSlidingMoves(state, from, color, PieceType.Rook, StraightOffsets);
    
    private IEnumerable<Move> GenerateBishopMoves(GameState state, Square from, Color color) =>
        GenerateSlidingMoves(state, from, color, PieceType.Bishop, DiagonalOffsets);
    
    private IEnumerable<Move> GenerateQueenMoves(GameState state, Square from, Color color)
    {
        var moves = new List<Move>();
        moves.AddRange(GenerateSlidingMoves(state, from, color, PieceType.Queen, StraightOffsets));
        moves.AddRange(GenerateSlidingMoves(state, from, color, PieceType.Queen, DiagonalOffsets));
        return moves;
    }
    
    private IEnumerable<Move> GenerateKingMoves(GameState state, Square from, Color color)
    {
        var moves = new List<Move>();
        
        // Regular king moves
        foreach (var offset in KingOffsets)
        {
            var targetIndex = from.Index + offset;
            if (targetIndex >= 0 && targetIndex < 64)
            {
                var target = new Square(targetIndex);
                
                // Check for valid king move (one square)
                if (Math.Abs(target.File - from.File) <= 1 && Math.Abs(target.Rank - from.Rank) <= 1)
                {
                    var targetPiece = state.GetPiece(target);
                    if (targetPiece.IsEmpty)
                    {
                        moves.Add(CreateMove(from, target, MoveKind.Quiet, PieceType.King, color));
                    }
                    else if (targetPiece.Color != color)
                    {
                        moves.Add(CreateMove(from, target, MoveKind.Capture, PieceType.King, color, PieceType.None, targetPiece.Type));
                    }
                }
            }
        }
        
        // Castling
        if (!IsInCheck(state, color))
        {
            // King-side castling
            if (state.CanCastle(color, true))
            {
                var kingTarget = new Square(6, color == Color.White ? 0 : 7);
                var rookSquare = new Square(7, color == Color.White ? 0 : 7);
                var f1 = new Square(5, color == Color.White ? 0 : 7);
                var g1 = new Square(6, color == Color.White ? 0 : 7);
                
                if (state.GetPiece(f1).IsEmpty && state.GetPiece(g1).IsEmpty &&
                    !SquareIsAttacked(state, f1, color == Color.White ? Color.Black : Color.White) &&
                    !SquareIsAttacked(state, g1, color == Color.White ? Color.Black : Color.White))
                {
                    moves.Add(CreateMove(from, kingTarget, MoveKind.CastleKingSide, PieceType.King, color));
                }
            }
            
            // Queen-side castling
            if (state.CanCastle(color, false))
            {
                var kingTarget = new Square(2, color == Color.White ? 0 : 7);
                var rookSquare = new Square(0, color == Color.White ? 0 : 7);
                var b1 = new Square(1, color == Color.White ? 0 : 7);
                var c1 = new Square(2, color == Color.White ? 0 : 7);
                var d1 = new Square(3, color == Color.White ? 0 : 7);
                
                if (state.GetPiece(b1).IsEmpty && state.GetPiece(c1).IsEmpty && state.GetPiece(d1).IsEmpty &&
                    !SquareIsAttacked(state, c1, color == Color.White ? Color.Black : Color.White) &&
                    !SquareIsAttacked(state, d1, color == Color.White ? Color.Black : Color.White))
                {
                    moves.Add(CreateMove(from, kingTarget, MoveKind.CastleQueenSide, PieceType.King, color));
                }
            }
        }
        
        return moves;
    }
    
    private Move CreateMove(Square from, Square to, MoveKind kind, PieceType movingPiece, Color movingColor,
        PieceType promotionTo = PieceType.None, PieceType capturedPiece = PieceType.None)
    {
        return new Move
        {
            From = from,
            To = to,
            Kind = kind,
            PromotionTo = promotionTo,
            MovingPiece = movingPiece,
            MovingColor = movingColor,
            CapturedPiece = capturedPiece
        };
    }
    
    public StateDelta ApplyMove(GameState state, Move move)
    {
        // Save state for undo
        var delta = new StateDelta
        {
            CapturedPiece = state.GetPiece(move.To),
            OldWhiteCastleK = state.WhiteCastleK,
            OldWhiteCastleQ = state.WhiteCastleQ,
            OldBlackCastleK = state.BlackCastleK,
            OldBlackCastleQ = state.BlackCastleQ,
            OldEnPassantFile = state.EnPassantFile,
            OldHalfmoveClock = state.HalfmoveClock,
            OldZobrist = state.Zobrist
        };
        
        var movingPiece = state.GetPiece(move.From);
        
        // Clear en passant
        state.EnPassantFile = null;
        
        // Handle different move types
        switch (move.Kind)
        {
            case MoveKind.Quiet:
                state.SetPiece(move.From, Piece.None);
                state.SetPiece(move.To, movingPiece);
                
                // Set en passant for double pawn push
                if (movingPiece.Type == PieceType.Pawn && Math.Abs(move.To.Rank - move.From.Rank) == 2)
                {
                    state.EnPassantFile = move.From.File;
                }
                break;
                
            case MoveKind.Capture:
                state.SetPiece(move.From, Piece.None);
                state.SetPiece(move.To, movingPiece);
                break;
                
            case MoveKind.EnPassant:
                state.SetPiece(move.From, Piece.None);
                state.SetPiece(move.To, movingPiece);
                // Remove captured pawn
                var capturedPawnSquare = new Square(move.To.File, move.From.Rank);
                state.SetPiece(capturedPawnSquare, Piece.None);
                break;
                
            case MoveKind.Promotion:
                state.SetPiece(move.From, Piece.None);
                state.SetPiece(move.To, new Piece(move.PromotionTo, movingPiece.Color));
                break;
                
            case MoveKind.CastleKingSide:
                // Move king
                state.SetPiece(move.From, Piece.None);
                state.SetPiece(move.To, movingPiece);
                // Move rook
                var kRook = new Square(7, move.From.Rank);
                var kRookTarget = new Square(5, move.From.Rank);
                var rookPiece = state.GetPiece(kRook);
                state.SetPiece(kRook, Piece.None);
                state.SetPiece(kRookTarget, rookPiece);
                break;
                
            case MoveKind.CastleQueenSide:
                // Move king
                state.SetPiece(move.From, Piece.None);
                state.SetPiece(move.To, movingPiece);
                // Move rook
                var qRook = new Square(0, move.From.Rank);
                var qRookTarget = new Square(3, move.From.Rank);
                var qRookPiece = state.GetPiece(qRook);
                state.SetPiece(qRook, Piece.None);
                state.SetPiece(qRookTarget, qRookPiece);
                break;
        }
        
        // Update castling rights
        if (movingPiece.Type == PieceType.King)
        {
            state.SetCastleRights(movingPiece.Color, true, false);
            state.SetCastleRights(movingPiece.Color, false, false);
        }
        else if (movingPiece.Type == PieceType.Rook)
        {
            if (move.From.File == 0) // Queen-side rook
                state.SetCastleRights(movingPiece.Color, false, false);
            else if (move.From.File == 7) // King-side rook
                state.SetCastleRights(movingPiece.Color, true, false);
        }
        
        // Update halfmove clock
        if (movingPiece.Type == PieceType.Pawn || move.IsCapture)
            state.HalfmoveClock = 0;
        else
            state.HalfmoveClock++;
        
        // Update fullmove number
        if (state.SideToMove == Color.Black)
            state.FullmoveNumber++;
        
        // Switch sides
        state.SideToMove = state.SideToMove == Color.White ? Color.Black : Color.White;
        
        return delta;
    }
    
    public void UnapplyMove(GameState state, Move move, StateDelta delta)
    {
        // Switch sides back
        state.SideToMove = state.SideToMove == Color.White ? Color.Black : Color.White;
        
        // Restore state
        state.WhiteCastleK = delta.OldWhiteCastleK;
        state.WhiteCastleQ = delta.OldWhiteCastleQ;
        state.BlackCastleK = delta.OldBlackCastleK;
        state.BlackCastleQ = delta.OldBlackCastleQ;
        state.EnPassantFile = delta.OldEnPassantFile;
        state.HalfmoveClock = delta.OldHalfmoveClock;
        state.Zobrist = delta.OldZobrist;
        
        if (state.SideToMove == Color.Black)
            state.FullmoveNumber--;
        
        var movingPiece = state.GetPiece(move.To);
        
        // Handle different move types in reverse
        switch (move.Kind)
        {
            case MoveKind.Quiet:
            case MoveKind.Capture:
                state.SetPiece(move.To, delta.CapturedPiece);
                state.SetPiece(move.From, new Piece(move.MovingPiece, move.MovingColor));
                break;
                
            case MoveKind.EnPassant:
                state.SetPiece(move.To, Piece.None);
                state.SetPiece(move.From, new Piece(PieceType.Pawn, move.MovingColor));
                // Restore captured pawn
                var capturedPawnSquare = new Square(move.To.File, move.From.Rank);
                state.SetPiece(capturedPawnSquare, new Piece(PieceType.Pawn, move.MovingColor == Color.White ? Color.Black : Color.White));
                break;
                
            case MoveKind.Promotion:
                state.SetPiece(move.To, delta.CapturedPiece);
                state.SetPiece(move.From, new Piece(PieceType.Pawn, move.MovingColor));
                break;
                
            case MoveKind.CastleKingSide:
                // Restore king
                state.SetPiece(move.To, Piece.None);
                state.SetPiece(move.From, new Piece(PieceType.King, move.MovingColor));
                // Restore rook
                var kRook = new Square(7, move.From.Rank);
                var kRookTarget = new Square(5, move.From.Rank);
                state.SetPiece(kRookTarget, Piece.None);
                state.SetPiece(kRook, new Piece(PieceType.Rook, move.MovingColor));
                break;
                
            case MoveKind.CastleQueenSide:
                // Restore king
                state.SetPiece(move.To, Piece.None);
                state.SetPiece(move.From, new Piece(PieceType.King, move.MovingColor));
                // Restore rook
                var qRook = new Square(0, move.From.Rank);
                var qRookTarget = new Square(3, move.From.Rank);
                state.SetPiece(qRookTarget, Piece.None);
                state.SetPiece(qRook, new Piece(PieceType.Rook, move.MovingColor));
                break;
        }
    }
}
