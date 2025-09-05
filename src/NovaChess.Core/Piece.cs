namespace NovaChess.Core;

public readonly struct Piece : IEquatable<Piece>
{
    public static readonly Piece None = new(PieceType.None, Color.White);
    
    public PieceType Type { get; }
    public Color Color { get; }
    
    public Piece(PieceType type, Color color)
    {
        Type = type;
        Color = color;
    }
    
    public bool IsEmpty => Type == PieceType.None;
    public bool IsWhite => Color == Color.White;
    public bool IsBlack => Color == Color.Black;
    
    public static bool operator ==(Piece left, Piece right) => 
        left.Type == right.Type && left.Color == right.Color;
    
    public static bool operator !=(Piece left, Piece right) => !(left == right);
    
    public bool Equals(Piece other) => Type == other.Type && Color == other.Color;
    
    public override bool Equals(object? obj) => obj is Piece other && Equals(other);
    
    public override int GetHashCode() => HashCode.Combine(Type, Color);
    
    public override string ToString() => Type == PieceType.None ? "-" : 
        $"{(IsWhite ? "w" : "b")}{Type.ToString()[0]}";
}
