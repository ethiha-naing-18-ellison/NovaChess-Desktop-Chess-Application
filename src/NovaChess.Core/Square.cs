namespace NovaChess.Core;

public readonly struct Square : IEquatable<Square>
{
    public static readonly Square None = new(-1);
    
    public int Index { get; }
    public int File => Index % 8;
    public int Rank => Index / 8;
    
    public Square(int index)
    {
        if (index < -1 || index > 63)
            throw new ArgumentOutOfRangeException(nameof(index));
        Index = index;
    }
    
    public Square(int file, int rank)
    {
        if (file < 0 || file > 7 || rank < 0 || rank > 7)
            throw new ArgumentOutOfRangeException($"Invalid file: {file}, rank: {rank}");
        Index = rank * 8 + file;
    }
    
    public static Square FromAlgebraic(string notation)
    {
        if (string.IsNullOrEmpty(notation) || notation.Length != 2)
            throw new ArgumentException("Invalid algebraic notation", nameof(notation));
            
        int file = char.ToLower(notation[0]) - 'a';
        int rank = notation[1] - '1';
        
        if (file < 0 || file > 7 || rank < 0 || rank > 7)
            throw new ArgumentException("Invalid algebraic notation", nameof(notation));
            
        return new Square(file, rank);
    }
    
    public string ToAlgebraic() => Index == -1 ? "-" : $"{(char)('a' + File)}{Rank + 1}";
    
    public bool IsValid => Index >= 0 && Index < 64;
    public bool IsLightSquare => (File + Rank) % 2 == 0;
    public bool IsDarkSquare => (File + Rank) % 2 == 1;
    
    public static bool operator ==(Square left, Square right) => left.Index == right.Index;
    public static bool operator !=(Square left, Square right) => left.Index != right.Index;
    
    public bool Equals(Square other) => Index == other.Index;
    public override bool Equals(object? obj) => obj is Square other && Equals(other);
    public override int GetHashCode() => Index.GetHashCode();
    public override string ToString() => ToAlgebraic();
}
