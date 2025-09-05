namespace NovaChess.Core;

public sealed class Arbiter
{
    private readonly IMoveGenerator _moveGenerator;
    private readonly GameAnalyzer _gameAnalyzer;
    
    public Arbiter(IMoveGenerator moveGenerator)
    {
        _moveGenerator = moveGenerator;
        _gameAnalyzer = new GameAnalyzer(moveGenerator);
    }
    
    public Arbiter() : this(new ChessMoveGenerator()) { }
    
    /// <summary>
    /// Get all legal moves for the current position
    /// </summary>
    public IReadOnlyList<Move> LegalMoves(GameState state)
    {
        return _moveGenerator.GenerateLegalMoves(state).ToList();
    }
    
    /// <summary>
    /// Get all legal moves from a specific square
    /// </summary>
    public IReadOnlyList<Move> LegalMovesFrom(GameState state, Square from)
    {
        return _moveGenerator.GenerateLegalMovesFrom(state, from).ToList();
    }
    
    /// <summary>
    /// Try to play a move. Returns true if successful, false if illegal.
    /// </summary>
    public bool TryPlay(GameState state, Move move)
    {
        // 1) Ensure the piece being moved belongs to the side to move
        var piece = state.GetPiece(move.From);
        if (piece.IsEmpty || piece.Color != state.SideToMove)
        {
            return false;
        }
        
        // 2) Ensure the move is in the list of legal moves
        var legalMoves = LegalMoves(state);
        if (!legalMoves.Any(m => MovesAreEqual(m, move)))
        {
            return false;
        }
        
        // Find the exact legal move (with all properties set correctly)
        var legalMove = legalMoves.First(m => MovesAreEqual(m, move));
        
        // 3) Apply the move
        var delta = _moveGenerator.ApplyMove(state, legalMove);
        state.History.Push((legalMove, delta));
        
        return true;
    }
    
    /// <summary>
    /// Try to play a move by coordinates. Returns the played move if successful, null if illegal.
    /// </summary>
    public Move? TryPlay(GameState state, Square from, Square to, PieceType promotionTo = PieceType.Queen)
    {
        var legalMoves = LegalMovesFrom(state, from);
        
        // Find matching move
        var matchingMove = legalMoves.FirstOrDefault(m => 
            m.To == to && 
            (!m.IsPromotion || m.PromotionTo == promotionTo));
        
        if (matchingMove == null)
            return null;
        
        var delta = _moveGenerator.ApplyMove(state, matchingMove);
        state.History.Push((matchingMove, delta));
        
        return matchingMove;
    }
    
    /// <summary>
    /// Undo the last move
    /// </summary>
    public bool TryUndo(GameState state)
    {
        if (state.History.Count == 0)
            return false;
        
        var (move, delta) = state.History.Pop();
        _moveGenerator.UnapplyMove(state, move, delta);
        return true;
    }
    
    /// <summary>
    /// Check if the current position is checkmate
    /// </summary>
    public bool IsCheckmate(GameState state)
    {
        return _gameAnalyzer.IsCheckmate(state);
    }
    
    /// <summary>
    /// Check if the current position is stalemate
    /// </summary>
    public bool IsStalemate(GameState state)
    {
        return _gameAnalyzer.IsStalemate(state);
    }
    
    /// <summary>
    /// Check if the current position is a draw by 50-move rule
    /// </summary>
    public bool IsDraw50MoveRule(GameState state)
    {
        return state.HalfmoveClock >= 100; // 50 moves = 100 half-moves
    }
    
    /// <summary>
    /// Check if the current position is a draw by insufficient material
    /// </summary>
    public bool IsDrawInsufficientMaterial(GameState state)
    {
        var whitePieces = new List<PieceType>();
        var blackPieces = new List<PieceType>();
        
        for (int i = 0; i < 64; i++)
        {
            var piece = state.Board64[i];
            if (!piece.IsEmpty && piece.Type != PieceType.King)
            {
                if (piece.Color == Color.White)
                    whitePieces.Add(piece.Type);
                else
                    blackPieces.Add(piece.Type);
            }
        }
        
        // K vs K
        if (whitePieces.Count == 0 && blackPieces.Count == 0)
            return true;
        
        // K+B vs K or K+N vs K
        if ((whitePieces.Count == 1 && blackPieces.Count == 0) ||
            (whitePieces.Count == 0 && blackPieces.Count == 1))
        {
            var piece = whitePieces.Count == 1 ? whitePieces[0] : blackPieces[0];
            return piece == PieceType.Bishop || piece == PieceType.Knight;
        }
        
        // K+B vs K+B (same color squares) - simplified check
        if (whitePieces.Count == 1 && blackPieces.Count == 1 &&
            whitePieces[0] == PieceType.Bishop && blackPieces[0] == PieceType.Bishop)
        {
            // This would require checking if bishops are on same color squares
            // For now, we'll be conservative and not call it a draw
            return false;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the current game result
    /// </summary>
    public GameResult GetGameResult(GameState state)
    {
        if (IsCheckmate(state))
        {
            return state.SideToMove == Color.White ? GameResult.BlackWins : GameResult.WhiteWins;
        }
        
        if (IsStalemate(state) || IsDraw50MoveRule(state) || IsDrawInsufficientMaterial(state))
        {
            return GameResult.Draw;
        }
        
        return GameResult.Ongoing;
    }
    
    /// <summary>
    /// Check if a move is in check
    /// </summary>
    public bool IsInCheck(GameState state, Color color)
    {
        return _moveGenerator.IsInCheck(state, color);
    }
    
    private bool MovesAreEqual(Move a, Move b)
    {
        return a.From == b.From && 
               a.To == b.To && 
               a.Kind == b.Kind &&
               a.PromotionTo == b.PromotionTo;
    }
    
    /// <summary>
    /// Analyze the current game state for checkmate, stalemate, draws
    /// </summary>
    public GameResult AnalyzePosition(GameState state)
    {
        return _gameAnalyzer.AnalyzePosition(state);
    }
    
    
    /// <summary>
    /// Get the specific draw reason if the game is a draw
    /// </summary>
    public DrawReason GetDrawReason(GameState state)
    {
        return _gameAnalyzer.GetDrawReason(state);
    }
    
    /// <summary>
    /// Get a human-readable description of the game result
    /// </summary>
    public string GetGameStatus(GameState state)
    {
        return _gameAnalyzer.GetResultDescription(state);
    }
    
    /// <summary>
    /// Check if the game is over (checkmate, stalemate, or draw)
    /// </summary>
    public bool IsGameOver(GameState state)
    {
        var result = _gameAnalyzer.AnalyzePosition(state);
        return result != GameResult.Ongoing;
    }
}
