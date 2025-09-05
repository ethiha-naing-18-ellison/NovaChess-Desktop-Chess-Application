namespace NovaChess.Core;

public sealed class Move : IEquatable<Move>
{
    public Square From { get; init; }
    public Square To { get; init; }
    public MoveKind Kind { get; init; }
    public PieceType PromotionTo { get; init; } = PieceType.None; // Q/R/B/N when Kind==Promotion
    
    // Additional info for move execution and undo
    public PieceType MovingPiece { get; init; }
    public Color MovingColor { get; init; }
    public PieceType CapturedPiece { get; init; } = PieceType.None;
    
    public Move() { }
    
    public Move(Square from, Square to, MoveKind kind = MoveKind.Quiet)
    {
        From = from;
        To = to;
        Kind = kind;
    }
    
    public bool IsCapture => Kind == MoveKind.Capture || Kind == MoveKind.EnPassant;
    public bool IsPromotion => Kind == MoveKind.Promotion;
    public bool IsCastling => Kind == MoveKind.CastleKingSide || Kind == MoveKind.CastleQueenSide;
    public bool IsEnPassant => Kind == MoveKind.EnPassant;
    
    public string ToSan()
    {
        if (IsCastling)
        {
            return Kind == MoveKind.CastleKingSide ? "O-O" : "O-O-O";
        }
        
        string move = "";
        
        // Piece letter (except for pawns)
        if (MovingPiece != PieceType.Pawn)
        {
            move += MovingPiece switch
            {
                PieceType.Knight => "N",
                PieceType.Bishop => "B",
                PieceType.Rook => "R",
                PieceType.Queen => "Q",
                PieceType.King => "K",
                _ => ""
            };
        }
        
        // Capture
        if (IsCapture)
        {
            if (MovingPiece == PieceType.Pawn)
                move += From.ToAlgebraic()[0];
            move += "x";
        }
        
        // Destination
        move += To.ToAlgebraic();
        
        // Promotion
        if (IsPromotion)
        {
            move += "=" + PromotionTo switch
            {
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                _ => ""
            };
        }
            
        return move;
    }
    
    public string ToUci() => $"{From.ToAlgebraic()}{To.ToAlgebraic()}{(IsPromotion ? PromotionTo.ToString().ToLower()[0] : "")}";
    
    public static bool operator ==(Move? left, Move? right) => 
        ReferenceEquals(left, right) || (left?.Equals(right) == true);
    
    public static bool operator !=(Move? left, Move? right) => !(left == right);
    
    public bool Equals(Move? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return From == other.From && 
               To == other.To && 
               Kind == other.Kind &&
               PromotionTo == other.PromotionTo;
    }
    
    public override bool Equals(object? obj) => obj is Move other && Equals(other);
    
    public override int GetHashCode() => HashCode.Combine(From, To, Kind, PromotionTo);
    
    public override string ToString() => ToSan();
}