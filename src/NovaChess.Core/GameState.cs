using System.Collections.Generic;

namespace NovaChess.Core;

public sealed class GameState
{
    public Piece[] Board64 { get; set; } = new Piece[64]; // a1=0 ... h8=63
    public Color SideToMove { get; set; } = Color.White;
    
    // Castling rights
    public bool WhiteCastleK { get; set; } = true;
    public bool WhiteCastleQ { get; set; } = true;
    public bool BlackCastleK { get; set; } = true;
    public bool BlackCastleQ { get; set; } = true;
    
    public int? EnPassantFile { get; set; } = null; // file 0..7 when available, rank implied by side
    public int HalfmoveClock { get; set; } = 0; // for 50-move rule
    public int FullmoveNumber { get; set; } = 1;
    public ulong Zobrist { get; set; } = 0; // for repetition detection
    
    public Stack<(Move move, StateDelta delta)> History { get; set; } = new();
    
    public GameState()
    {
        InitializeStartingPosition();
    }
    
    public GameState(GameState other)
    {
        Array.Copy(other.Board64, Board64, 64);
        SideToMove = other.SideToMove;
        WhiteCastleK = other.WhiteCastleK;
        WhiteCastleQ = other.WhiteCastleQ;
        BlackCastleK = other.BlackCastleK;
        BlackCastleQ = other.BlackCastleQ;
        EnPassantFile = other.EnPassantFile;
        HalfmoveClock = other.HalfmoveClock;
        FullmoveNumber = other.FullmoveNumber;
        Zobrist = other.Zobrist;
        History = new Stack<(Move, StateDelta)>(other.History.Reverse());
    }
    
    public Piece GetPiece(Square square) => 
        square.IsValid ? Board64[square.Index] : Piece.None;
    
    public void SetPiece(Square square, Piece piece)
    {
        if (square.IsValid)
            Board64[square.Index] = piece;
    }
    
    public void SetPiece(Square square, PieceType type, Color color)
    {
        SetPiece(square, new Piece(type, color));
    }
    
    private void InitializeStartingPosition()
    {
        // Clear board
        for (int i = 0; i < 64; i++)
            Board64[i] = Piece.None;
        
        // White pieces
        SetPiece(new Square(0, 0), PieceType.Rook, Color.White);   // a1
        SetPiece(new Square(1, 0), PieceType.Knight, Color.White); // b1
        SetPiece(new Square(2, 0), PieceType.Bishop, Color.White); // c1
        SetPiece(new Square(3, 0), PieceType.Queen, Color.White);  // d1
        SetPiece(new Square(4, 0), PieceType.King, Color.White);   // e1
        SetPiece(new Square(5, 0), PieceType.Bishop, Color.White); // f1
        SetPiece(new Square(6, 0), PieceType.Knight, Color.White); // g1
        SetPiece(new Square(7, 0), PieceType.Rook, Color.White);   // h1
        
        for (int file = 0; file < 8; file++)
            SetPiece(new Square(file, 1), PieceType.Pawn, Color.White);
        
        // Black pieces
        SetPiece(new Square(0, 7), PieceType.Rook, Color.Black);   // a8
        SetPiece(new Square(1, 7), PieceType.Knight, Color.Black); // b8
        SetPiece(new Square(2, 7), PieceType.Bishop, Color.Black); // c8
        SetPiece(new Square(3, 7), PieceType.Queen, Color.Black);  // d8
        SetPiece(new Square(4, 7), PieceType.King, Color.Black);   // e8
        SetPiece(new Square(5, 7), PieceType.Bishop, Color.Black); // f8
        SetPiece(new Square(6, 7), PieceType.Knight, Color.Black); // g8
        SetPiece(new Square(7, 7), PieceType.Rook, Color.Black);   // h8
        
        for (int file = 0; file < 8; file++)
            SetPiece(new Square(file, 6), PieceType.Pawn, Color.Black);
    }
    
    public Square FindKing(Color color)
    {
        for (int i = 0; i < 64; i++)
        {
            var piece = Board64[i];
            if (piece.Type == PieceType.King && piece.Color == color)
                return new Square(i);
        }
        return Square.None;
    }
    
    public bool CanCastle(Color color, bool kingSide)
    {
        return color switch
        {
            Color.White => kingSide ? WhiteCastleK : WhiteCastleQ,
            Color.Black => kingSide ? BlackCastleK : BlackCastleQ,
            _ => false
        };
    }
    
    public void SetCastleRights(Color color, bool kingSide, bool value)
    {
        switch (color)
        {
            case Color.White:
                if (kingSide) WhiteCastleK = value;
                else WhiteCastleQ = value;
                break;
            case Color.Black:
                if (kingSide) BlackCastleK = value;
                else BlackCastleQ = value;
                break;
        }
    }
    
    public string ToFen()
    {
        // Simple FEN implementation for debugging
        var board = "";
        for (int rank = 7; rank >= 0; rank--)
        {
            int empty = 0;
            for (int file = 0; file < 8; file++)
            {
                var square = new Square(rank * 8 + file);
                var piece = GetPiece(square);
                
                if (piece.IsEmpty)
                {
                    empty++;
                }
                else
                {
                    if (empty > 0)
                    {
                        board += empty.ToString();
                        empty = 0;
                    }
                    
                    var symbol = piece.Type switch
                    {
                        PieceType.Pawn => "p",
                        PieceType.Rook => "r",
                        PieceType.Knight => "n",
                        PieceType.Bishop => "b",
                        PieceType.Queen => "q",
                        PieceType.King => "k",
                        _ => "?"
                    };
                    
                    if (piece.IsWhite)
                        symbol = symbol.ToUpper();
                    
                    board += symbol;
                }
            }
            
            if (empty > 0)
                board += empty.ToString();
            
            if (rank > 0)
                board += "/";
        }
        
        var turn = SideToMove == Color.White ? "w" : "b";
        var castling = "";
        if (WhiteCastleK) castling += "K";
        if (WhiteCastleQ) castling += "Q";
        if (BlackCastleK) castling += "k";
        if (BlackCastleQ) castling += "q";
        if (string.IsNullOrEmpty(castling)) castling = "-";
        
        var enPassant = EnPassantFile.HasValue ? $"{(char)('a' + EnPassantFile.Value)}{(SideToMove == Color.White ? 6 : 3)}" : "-";
        
        return $"{board} {turn} {castling} {enPassant} {HalfmoveClock} {FullmoveNumber}";
    }
    
    public void LoadFromFen(string fen)
    {
        // Simple FEN loading - reset to starting position for now
        var parts = fen.Split(' ');
        if (parts.Length < 4) return;
        
        // Clear board
        for (int i = 0; i < 64; i++)
        {
            Board64[i] = Piece.None;
        }
        
        // Parse board position
        var boardStr = parts[0];
        int square = 56; // Start at a8 (top-left)
        
        foreach (char c in boardStr)
        {
            if (c == '/')
            {
                square -= 16; // Move to next rank (8 squares back, then 8 more to start of rank)
            }
            else if (char.IsDigit(c))
            {
                square += (c - '0'); // Skip empty squares
            }
            else
            {
                // Parse piece
                var pieceType = char.ToLower(c) switch
                {
                    'p' => PieceType.Pawn,
                    'r' => PieceType.Rook,
                    'n' => PieceType.Knight,
                    'b' => PieceType.Bishop,
                    'q' => PieceType.Queen,
                    'k' => PieceType.King,
                    _ => PieceType.None
                };
                
                if (pieceType != PieceType.None)
                {
                    var color = char.IsUpper(c) ? Color.White : Color.Black;
                    Board64[square] = new Piece(pieceType, color);
                    square++;
                }
            }
        }
        
        // Parse side to move
        if (parts.Length > 1)
        {
            SideToMove = parts[1] == "w" ? Color.White : Color.Black;
        }
        
        // Parse castling rights
        if (parts.Length > 2)
        {
            var castling = parts[2];
            WhiteCastleK = castling.Contains('K');
            WhiteCastleQ = castling.Contains('Q');
            BlackCastleK = castling.Contains('k');
            BlackCastleQ = castling.Contains('q');
        }
        
        // Parse en passant
        if (parts.Length > 3 && parts[3] != "-")
        {
            EnPassantFile = parts[3][0] - 'a';
        }
        else
        {
            EnPassantFile = null;
        }
        
        // Parse move counters
        if (parts.Length > 4)
        {
            int.TryParse(parts[4], out var halfmove);
            HalfmoveClock = halfmove;
        }
        
        if (parts.Length > 5)
        {
            int.TryParse(parts[5], out var fullmove);
            FullmoveNumber = fullmove;
        }
    }
}

public sealed class StateDelta
{
    public Piece CapturedPiece { get; init; } = Piece.None;
    public bool OldWhiteCastleK { get; init; }
    public bool OldWhiteCastleQ { get; init; }
    public bool OldBlackCastleK { get; init; }
    public bool OldBlackCastleQ { get; init; }
    public int? OldEnPassantFile { get; init; }
    public int OldHalfmoveClock { get; init; }
    public ulong OldZobrist { get; init; }
}
