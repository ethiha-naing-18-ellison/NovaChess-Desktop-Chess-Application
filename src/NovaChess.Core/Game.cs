namespace NovaChess.Core;

public class Game
{
    private readonly Board _board;
    private readonly MoveGenerator _moveGenerator;
    private readonly List<Move> _moveHistory = new();
    private readonly List<ulong> _positionHistory = new(); // Zobrist hashes for repetition detection
    
    public Board Board => _board;
    public IReadOnlyList<Move> MoveHistory => _moveHistory.AsReadOnly();
    public GameResult Result { get; private set; } = GameResult.Ongoing;
    public DrawReason DrawReason { get; private set; } = DrawReason.None;
    
    public event EventHandler<GameStateChangedEventArgs>? GameStateChanged;
    
    public Game()
    {
        _board = new Board();
        _moveGenerator = new MoveGenerator(_board);
        _positionHistory.Add(_board.ZobristHash);
    }
    
    public Game(string fen) : this()
    {
        _board.LoadFromFen(fen);
        _positionHistory.Clear();
        _positionHistory.Add(_board.ZobristHash);
    }
    
    public List<Move> GetLegalMoves()
    {
        return _moveGenerator.GenerateLegalMoves();
    }
    
    public List<Move> GetLegalMovesFrom(Square square)
    {
        var allMoves = GetLegalMoves();
        return allMoves.Where(m => m.From == square).ToList();
    }
    
    public bool IsMoveLegal(Move move)
    {
        var legalMoves = GetLegalMoves();
        return legalMoves.Any(m => m.Equals(move));
    }
    
    public bool MakeMove(Move move)
    {
        if (!IsMoveLegal(move))
            return false;
            
        // Store current state for undo
        var currentHash = _board.ZobristHash;
        
        // Execute the move
        ExecuteMove(move);
        
        // Add to history
        _moveHistory.Add(move);
        _positionHistory.Add(_board.ZobristHash);
        
        // Check game result
        CheckGameResult();
        
        // Notify listeners
        GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(move, Result, DrawReason));
        
        return true;
    }
    
    private void ExecuteMove(Move move)
    {
        var (piece, color) = _board.GetPiece(move.From);
        var (capturedPiece, capturedColor) = _board.GetPiece(move.To);
        
        // Remove piece from source
        _board.SetPiece(move.From, PieceType.None, PieceColor.White);
        
        // Place piece at destination (or promotion piece)
        var targetPiece = move.PromotionPiece != PieceType.None ? move.PromotionPiece : piece;
        _board.SetPiece(move.To, targetPiece, color);
        
        // Handle special moves
        if (move.IsCastle)
        {
            ExecuteCastle(move, color);
        }
        else if (move.IsEnPassant)
        {
            // Remove captured pawn
            var capturedPawnSquare = new Square(move.To.File, move.From.Rank);
            _board.SetPiece(capturedPawnSquare, PieceType.None, PieceColor.White);
        }
        
        // Update castling rights
        UpdateCastlingRights(move, piece, move.From);
        
        // Update en passant square
        if (piece == PieceType.Pawn && Math.Abs(move.To.Rank - move.From.Rank) == 2)
        {
            var enPassantRank = (move.From.Rank + move.To.Rank) / 2;
            _board.EnPassantSquare = new Square(move.From.File, enPassantRank);
        }
        else
        {
            _board.EnPassantSquare = Square.None;
        }
        
        // Update halfmove clock
        if (piece == PieceType.Pawn || capturedPiece != PieceType.None)
        {
            _board.HalfMoveClock = 0;
        }
        else
        {
            _board.HalfMoveClock++;
        }
        
        // Update fullmove number
        if (_board.SideToMove == PieceColor.Black)
        {
            _board.FullMoveNumber++;
        }
        
        // Switch sides
        _board.SideToMove = _board.SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
        
        // Update Zobrist hash
        _board.UpdateZobristHash();
    }
    
    private void ExecuteCastle(Move move, PieceColor color)
    {
        if (color == PieceColor.White)
        {
            if (move.To.File == 6) // Kingside
            {
                // Move rook from h1 to f1
                _board.SetPiece(new Square(7, 0), PieceType.None, PieceColor.White);
                _board.SetPiece(new Square(5, 0), PieceType.Rook, PieceColor.White);
            }
            else // Queenside
            {
                // Move rook from a1 to d1
                _board.SetPiece(new Square(0, 0), PieceType.None, PieceColor.White);
                _board.SetPiece(new Square(3, 0), PieceType.Rook, PieceColor.White);
            }
        }
        else
        {
            if (move.To.File == 6) // Kingside
            {
                // Move rook from h8 to f8
                _board.SetPiece(new Square(7, 7), PieceType.None, PieceColor.Black);
                _board.SetPiece(new Square(5, 7), PieceType.Rook, PieceColor.Black);
            }
            else // Queenside
            {
                // Move rook from a8 to d8
                _board.SetPiece(new Square(0, 7), PieceType.None, PieceColor.Black);
                _board.SetPiece(new Square(3, 7), PieceType.Rook, PieceColor.Black);
            }
        }
    }
    
    private void UpdateCastlingRights(Move move, PieceType piece, Square from)
    {
        if (piece == PieceType.King)
        {
            if (move.From.File == 4) // King moved from e-file
            {
                if (move.From.Rank == 0) // White king
                {
                    _board.CastleRights &= ~(CastleRights.WhiteKingside | CastleRights.WhiteQueenside);
                }
                else // Black king
                {
                    _board.CastleRights &= ~(CastleRights.BlackKingside | CastleRights.BlackQueenside);
                }
            }
        }
        else if (piece == PieceType.Rook)
        {
            if (move.From.File == 0) // Rook moved from a-file
            {
                if (move.From.Rank == 0) // White queenside rook
                {
                    _board.CastleRights &= ~CastleRights.WhiteQueenside;
                }
                else // Black queenside rook
                {
                    _board.CastleRights &= ~CastleRights.BlackQueenside;
                }
            }
            else if (move.From.File == 7) // Rook moved from h-file
            {
                if (move.From.Rank == 0) // White kingside rook
                {
                    _board.CastleRights &= ~CastleRights.WhiteKingside;
                }
                else // Black kingside rook
                {
                    _board.CastleRights &= ~CastleRights.BlackKingside;
                }
            }
        }
    }
    
    private void CheckGameResult()
    {
        var legalMoves = GetLegalMoves();
        var isInCheck = IsKingInCheck(_board.SideToMove);
        
        if (legalMoves.Count == 0)
        {
            if (isInCheck)
            {
                Result = _board.SideToMove == PieceColor.White ? GameResult.BlackWins : GameResult.WhiteWins;
            }
            else
            {
                Result = GameResult.Draw;
                DrawReason = DrawReason.Stalemate;
            }
        }
        else if (isInCheck)
        {
            // Check if it's checkmate (no legal moves that get out of check)
            var canEscapeCheck = false;
            foreach (var move in legalMoves)
            {
                // This is a simplified check - in production you'd want to make the move temporarily
                // and check if the king is still in check
                canEscapeCheck = true;
                break;
            }
            
            if (!canEscapeCheck)
            {
                Result = _board.SideToMove == PieceColor.White ? GameResult.BlackWins : GameResult.WhiteWins;
            }
        }
        else
        {
            // Check for draws
            if (CheckThreefoldRepetition())
            {
                Result = GameResult.Draw;
                DrawReason = DrawReason.ThreefoldRepetition;
            }
            else if (_board.HalfMoveClock >= 50)
            {
                Result = GameResult.Draw;
                DrawReason = DrawReason.FiftyMoveRule;
            }
            else if (CheckInsufficientMaterial())
            {
                Result = GameResult.Draw;
                DrawReason = DrawReason.InsufficientMaterial;
            }
        }
    }
    
    private bool IsKingInCheck(PieceColor color)
    {
        // Find the king
        var kingSquare = FindKing(color);
        if (!kingSquare.IsValid)
            return false;
            
        // Check if any opponent piece can attack the king
        var opponentColor = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        
        // This is a simplified implementation - in production you'd want to optimize this
        for (int i = 0; i < 64; i++)
        {
            var square = new Square(i);
            var (piece, pieceColor) = _board.GetPiece(square);
            
            if (piece != PieceType.None && pieceColor == opponentColor)
            {
                // Check if this piece can attack the king
                var moves = piece switch
                {
                    PieceType.Pawn => GeneratePawnAttacks(square, pieceColor),
                    PieceType.Knight => GenerateKnightAttacks(square),
                    PieceType.Bishop => GenerateBishopAttacks(square, pieceColor),
                    PieceType.Rook => GenerateRookAttacks(square, pieceColor),
                    PieceType.Queen => GenerateQueenAttacks(square, pieceColor),
                    PieceType.King => GenerateKingAttacks(square, pieceColor),
                    _ => new List<Square>()
                };
                
                if (moves.Contains(kingSquare))
                    return true;
            }
        }
        
        return false;
    }
    
    private Square FindKing(PieceColor color)
    {
        for (int i = 0; i < 64; i++)
        {
            var square = new Square(i);
            var (piece, pieceColor) = _board.GetPiece(square);
            if (piece == PieceType.King && pieceColor == color)
                return square;
        }
        return Square.None;
    }
    
    private bool CheckThreefoldRepetition()
    {
        var currentHash = _board.ZobristHash;
        int count = _positionHistory.Count(h => h == currentHash);
        return count >= 3;
    }
    
    private bool CheckInsufficientMaterial()
    {
        var whitePieces = new Dictionary<PieceType, int>();
        var blackPieces = new Dictionary<PieceType, int>();
        
        // Count pieces
        for (int i = 0; i < 64; i++)
        {
            var square = new Square(i);
            var (piece, color) = _board.GetPiece(square);
            if (piece != PieceType.None)
            {
                var pieces = color == PieceColor.White ? whitePieces : blackPieces;
                pieces[piece] = pieces.GetValueOrDefault(piece, 0) + 1;
            }
        }
        
        // Check for insufficient material patterns
        if (whitePieces.Count == 1 && blackPieces.Count == 1)
        {
            if (whitePieces.ContainsKey(PieceType.King) && blackPieces.ContainsKey(PieceType.King))
                return true; // K vs K
        }
        
        // Add more insufficient material patterns here
        // K vs K+B, K vs K+N, K+B vs K+B (same color bishops), etc.
        
        return false;
    }
    
    // Simplified attack generation methods for check detection
    private List<Square> GeneratePawnAttacks(Square square, PieceColor color) { /* Implementation */ return new(); }
    private List<Square> GenerateKnightAttacks(Square square) { /* Implementation */ return new(); }
    private List<Square> GenerateBishopAttacks(Square square, PieceColor color) { /* Implementation */ return new(); }
    private List<Square> GenerateRookAttacks(Square square, PieceColor color) { /* Implementation */ return new(); }
    private List<Square> GenerateQueenAttacks(Square square, PieceColor color) { /* Implementation */ return new(); }
    private List<Square> GenerateKingAttacks(Square square, PieceColor color) { /* Implementation */ return new(); }
    
    public void Reset()
    {
        _board.SetStartingPosition();
        _moveHistory.Clear();
        _positionHistory.Clear();
        _positionHistory.Add(_board.ZobristHash);
        Result = GameResult.Ongoing;
        DrawReason = DrawReason.None;
        
        GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(null, Result, DrawReason));
    }
    
    public string GetFen()
    {
        return _board.ToFen();
    }
    
    public void LoadFromFen(string fen)
    {
        _board.LoadFromFen(fen);
        _moveHistory.Clear();
        _positionHistory.Clear();
        _positionHistory.Add(_board.ZobristHash);
        Result = GameResult.Ongoing;
        DrawReason = DrawReason.None;
        
        GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(null, Result, DrawReason));
    }
}

public class GameStateChangedEventArgs : EventArgs
{
    public Move? LastMove { get; }
    public GameResult Result { get; }
    public DrawReason DrawReason { get; }
    
    public GameStateChangedEventArgs(Move? lastMove, GameResult result, DrawReason drawReason)
    {
        LastMove = lastMove;
        Result = result;
        DrawReason = drawReason;
    }
}
