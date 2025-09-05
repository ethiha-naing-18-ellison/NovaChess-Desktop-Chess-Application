using System.Numerics;

namespace NovaChess.Core;

public class Board
{
    // Bitboards for each piece type and color
    private readonly ulong[] _pieceBitboards = new ulong[12]; // 6 piece types × 2 colors
    private readonly ulong[] _colorBitboards = new ulong[2]; // White and Black pieces
    
    // Game state
    public PieceColor SideToMove { get; set; }
    public CastleRights CastleRights { get; set; }
    public Square EnPassantSquare { get; set; }
    public int HalfMoveClock { get; set; }
    public int FullMoveNumber { get; set; }
    
    // Zobrist hash for repetition detection
    public ulong ZobristHash { get; private set; }
    
    // Move history for undo/redo
    private readonly Stack<MoveInfo> _moveHistory = new();
    
    public Board()
    {
        SetStartingPosition();
    }
    
    public Board(string fen) : this()
    {
        LoadFromFen(fen);
    }
    
    public void SetStartingPosition()
    {
        // Clear all bitboards
        Array.Clear(_pieceBitboards);
        Array.Clear(_colorBitboards);
        
        // Set up starting position
        // White pieces
        SetPiece(Square.FromAlgebraic("a1"), PieceType.Rook, PieceColor.White);
        SetPiece(Square.FromAlgebraic("b1"), PieceType.Knight, PieceColor.White);
        SetPiece(Square.FromAlgebraic("c1"), PieceType.Bishop, PieceColor.White);
        SetPiece(Square.FromAlgebraic("d1"), PieceType.Queen, PieceColor.White);
        SetPiece(Square.FromAlgebraic("e1"), PieceType.King, PieceColor.White);
        SetPiece(Square.FromAlgebraic("f1"), PieceType.Bishop, PieceColor.White);
        SetPiece(Square.FromAlgebraic("g1"), PieceType.Knight, PieceColor.White);
        SetPiece(Square.FromAlgebraic("h1"), PieceType.Rook, PieceColor.White);
        
        // White pawns
        for (int file = 0; file < 8; file++)
        {
            SetPiece(new Square(file, 1), PieceType.Pawn, PieceColor.White);
        }
        
        // Black pieces
        SetPiece(Square.FromAlgebraic("a8"), PieceType.Rook, PieceColor.Black);
        SetPiece(Square.FromAlgebraic("b8"), PieceType.Knight, PieceColor.Black);
        SetPiece(Square.FromAlgebraic("c8"), PieceType.Bishop, PieceColor.Black);
        SetPiece(Square.FromAlgebraic("d8"), PieceType.Queen, PieceColor.Black);
        SetPiece(Square.FromAlgebraic("e8"), PieceType.King, PieceColor.Black);
        SetPiece(Square.FromAlgebraic("f8"), PieceType.Bishop, PieceColor.Black);
        SetPiece(Square.FromAlgebraic("g8"), PieceType.Knight, PieceColor.Black);
        SetPiece(Square.FromAlgebraic("h8"), PieceType.Rook, PieceColor.Black);
        
        // Black pawns
        for (int file = 0; file < 8; file++)
        {
            SetPiece(new Square(file, 6), PieceType.Pawn, PieceColor.Black);
        }
        
        // Game state
        SideToMove = PieceColor.White;
        CastleRights = CastleRights.All;
        EnPassantSquare = Square.None;
        HalfMoveClock = 0;
        FullMoveNumber = 1;
        
        UpdateZobristHash();
    }
    
    public void SetPiece(Square square, PieceType piece, PieceColor color)
    {
        if (!square.IsValid || piece == PieceType.None)
            return;
            
        int pieceIndex = GetPieceIndex(piece, color);
        ulong squareBit = 1UL << square.Index;
        
        // Clear any existing piece at this square
        for (int i = 0; i < 12; i++)
        {
            if ((_pieceBitboards[i] & squareBit) != 0)
            {
                _pieceBitboards[i] &= ~squareBit;
            }
        }
        
        // Set the new piece
        _pieceBitboards[pieceIndex] |= squareBit;
        _colorBitboards[(int)color] |= squareBit;
    }
    
    public (PieceType piece, PieceColor color) GetPiece(Square square)
    {
        if (!square.IsValid)
            return (PieceType.None, PieceColor.White);
            
        ulong squareBit = 1UL << square.Index;
        
        for (int i = 0; i < 12; i++)
        {
            if ((_pieceBitboards[i] & squareBit) != 0)
            {
                PieceType piece = (PieceType)(i % 6 + 1);
                PieceColor color = (PieceColor)(i / 6);
                return (piece, color);
            }
        }
        
        return (PieceType.None, PieceColor.White);
    }
    
    public bool IsSquareOccupied(Square square)
    {
        if (!square.IsValid)
            return false;
            
        ulong squareBit = 1UL << square.Index;
        return ((_colorBitboards[0] | _colorBitboards[1]) & squareBit) != 0;
    }
    
    public bool IsSquareOccupiedByColor(Square square, PieceColor color)
    {
        if (!square.IsValid)
            return false;
            
        ulong squareBit = 1UL << square.Index;
        return (_colorBitboards[(int)color] & squareBit) != 0;
    }
    
    public ulong GetPieceBitboard(PieceType piece, PieceColor color)
    {
        return _pieceBitboards[GetPieceIndex(piece, color)];
    }
    
    public ulong GetColorBitboard(PieceColor color)
    {
        return _colorBitboards[(int)color];
    }
    
    public ulong GetAllPiecesBitboard()
    {
        return _colorBitboards[0] | _colorBitboards[1];
    }
    
    private static int GetPieceIndex(PieceType piece, PieceColor color)
    {
        return (int)color * 6 + (int)piece - 1;
    }
    
    public void UpdateZobristHash()
    {
        // Simple Zobrist hash implementation
        // In a production version, you'd use pre-computed random numbers
        ZobristHash = 0;
        
        for (int i = 0; i < 64; i++)
        {
            var square = new Square(i);
            var (piece, color) = GetPiece(square);
            if (piece != PieceType.None)
            {
                ZobristHash ^= (ulong)((int)piece * 100 + (int)color * 10 + i);
            }
        }
        
        // Include other state in hash
        ZobristHash ^= (ulong)SideToMove;
        ZobristHash ^= (ulong)CastleRights << 8;
        if (EnPassantSquare.IsValid)
            ZobristHash ^= (ulong)(EnPassantSquare.Index << 16);
    }
    
    public void LoadFromFen(string fen)
    {
        var parts = fen.Split(' ');
        if (parts.Length < 4)
            throw new ArgumentException("Invalid FEN string", nameof(fen));
            
        // Clear board
        Array.Clear(_pieceBitboards);
        Array.Clear(_colorBitboards);
        
        // Parse piece placement
        var ranks = parts[0].Split('/');
        if (ranks.Length != 8)
            throw new ArgumentException("Invalid FEN piece placement", nameof(fen));
            
        for (int rank = 0; rank < 8; rank++)
        {
            int file = 0;
            foreach (char c in ranks[7 - rank])
            {
                if (char.IsDigit(c))
                {
                    file += c - '0';
                }
                else
                {
                    var piece = char.ToUpper(c) switch
                    {
                        'P' => PieceType.Pawn,
                        'N' => PieceType.Knight,
                        'B' => PieceType.Bishop,
                        'R' => PieceType.Rook,
                        'Q' => PieceType.Queen,
                        'K' => PieceType.King,
                        _ => throw new ArgumentException($"Invalid piece character: {c}", nameof(fen))
                    };
                    
                    var color = char.IsUpper(c) ? PieceColor.White : PieceColor.Black;
                    SetPiece(new Square(file, rank), piece, color);
                    file++;
                }
            }
        }
        
        // Parse active color
        SideToMove = parts[1] switch
        {
            "w" => PieceColor.White,
            "b" => PieceColor.Black,
            _ => throw new ArgumentException($"Invalid active color: {parts[1]}", nameof(fen))
        };
        
        // Parse castling rights
        CastleRights = CastleRights.None;
        if (parts[2] != "-")
        {
            foreach (char c in parts[2])
            {
                CastleRights |= c switch
                {
                    'K' => CastleRights.WhiteKingside,
                    'Q' => CastleRights.WhiteQueenside,
                    'k' => CastleRights.BlackKingside,
                    'q' => CastleRights.BlackQueenside,
                    _ => CastleRights.None
                };
            }
        }
        
        // Parse en passant square
        EnPassantSquare = parts[3] == "-" ? Square.None : Square.FromAlgebraic(parts[3]);
        
        // Parse halfmove clock
        HalfMoveClock = parts.Length > 4 ? int.Parse(parts[4]) : 0;
        
        // Parse fullmove number
        FullMoveNumber = parts.Length > 5 ? int.Parse(parts[5]) : 1;
        
        UpdateZobristHash();
    }
    
    public string ToFen()
    {
        var fen = new System.Text.StringBuilder();
        
        // Piece placement
        for (int rank = 7; rank >= 0; rank--)
        {
            int emptyCount = 0;
            for (int file = 0; file < 8; file++)
            {
                var square = new Square(file, rank);
                var (piece, color) = GetPiece(square);
                
                if (piece == PieceType.None)
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        fen.Append(emptyCount);
                        emptyCount = 0;
                    }
                    
                    var pieceChar = piece switch
                    {
                        PieceType.Pawn => 'p',
                        PieceType.Knight => 'n',
                        PieceType.Bishop => 'b',
                        PieceType.Rook => 'r',
                        PieceType.Queen => 'q',
                        PieceType.King => 'k',
                        _ => '?'
                    };
                    
                    if (color == PieceColor.White)
                        pieceChar = char.ToUpper(pieceChar);
                        
                    fen.Append(pieceChar);
                }
            }
            
            if (emptyCount > 0)
                fen.Append(emptyCount);
                
            if (rank > 0)
                fen.Append('/');
        }
        
        // Active color
        fen.Append(' ').Append(SideToMove == PieceColor.White ? 'w' : 'b');
        
        // Castling rights
        fen.Append(' ');
        if (CastleRights == CastleRights.None)
        {
            fen.Append('-');
        }
        else
        {
            if ((CastleRights & CastleRights.WhiteKingside) != 0) fen.Append('K');
            if ((CastleRights & CastleRights.WhiteQueenside) != 0) fen.Append('Q');
            if ((CastleRights & CastleRights.BlackKingside) != 0) fen.Append('k');
            if ((CastleRights & CastleRights.BlackQueenside) != 0) fen.Append('q');
        }
        
        // En passant square
        fen.Append(' ').Append(EnPassantSquare.IsValid ? EnPassantSquare.ToAlgebraic() : "-");
        
        // Halfmove clock
        fen.Append(' ').Append(HalfMoveClock);
        
        // Fullmove number
        fen.Append(' ').Append(FullMoveNumber);
        
        return fen.ToString();
    }
    
    private record MoveInfo(
        Square From, 
        Square To, 
        PieceType Piece, 
        PieceType CapturedPiece, 
        PieceColor CapturedColor,
        CastleRights CastleRights,
        Square EnPassantSquare,
        int HalfMoveClock,
        ulong ZobristHash
    );
}
