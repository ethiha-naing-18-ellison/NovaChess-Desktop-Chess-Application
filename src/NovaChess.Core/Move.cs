namespace NovaChess.Core;

public struct Move : IEquatable<Move>
{
    public Square From { get; }
    public Square To { get; }
    public PieceType Piece { get; }
    public PieceType PromotionPiece { get; }
    public bool IsCapture { get; set; }
    public bool IsEnPassant { get; set; }
    public bool IsCastle { get; set; }
    public bool IsDoublePawnPush { get; set; }
    public bool IsCheck { get; set; }
    public bool IsCheckmate { get; set; }
    
    public Move(Square from, Square to, PieceType piece, PieceType promotionPiece = PieceType.None)
    {
        From = from;
        To = to;
        Piece = piece;
        PromotionPiece = promotionPiece;
        
        // These will be set by the move generator
        IsCapture = false;
        IsEnPassant = false;
        IsCastle = false;
        IsDoublePawnPush = false;
        IsCheck = false;
        IsCheckmate = false;
    }
    
    public Move WithFlags(bool isCapture, bool isEnPassant, bool isCastle, bool isDoublePawnPush, bool isCheck, bool isCheckmate)
    {
        return new Move(From, To, Piece, PromotionPiece)
        {
            IsCapture = isCapture,
            IsEnPassant = isEnPassant,
            IsCastle = isCastle,
            IsDoublePawnPush = isDoublePawnPush,
            IsCheck = isCheck,
            IsCheckmate = isCheckmate
        };
    }
    
    public string ToSan()
    {
        if (IsCastle)
        {
            return To.File == 6 ? "O-O" : "O-O-O";
        }
        
        string move = "";
        
        // Piece letter (except for pawns)
        if (Piece != PieceType.Pawn)
        {
            move += Piece switch
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
            if (Piece == PieceType.Pawn)
                move += From.ToAlgebraic()[0];
            move += "x";
        }
        
        // Destination
        move += To.ToAlgebraic();
        
        // Promotion
        if (PromotionPiece != PieceType.None)
        {
            move += "=" + PromotionPiece switch
            {
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                _ => ""
            };
        }
        
        // Check/Checkmate
        if (IsCheckmate)
            move += "#";
        else if (IsCheck)
            move += "+";
            
        return move;
    }
    
    public string ToUci() => $"{From.ToAlgebraic()}{To.ToAlgebraic()}{(PromotionPiece != PieceType.None ? PromotionPiece.ToString().ToLower()[0] : "")}";
    
    public static bool operator ==(Move left, Move right) => left.Equals(right);
    public static bool operator !=(Move left, Move right) => !left.Equals(right);
    
    public bool Equals(Move other) => 
        From == other.From && 
        To == other.To && 
        Piece == other.Piece && 
        PromotionPiece == other.PromotionPiece;
        
    public override bool Equals(object? obj) => obj is Move other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(From, To, Piece, PromotionPiece);
    public override string ToString() => ToSan();
}
