namespace NovaChess.Core;

public class Game
{
    private readonly GameState _gameState;
    private readonly Arbiter _arbiter;
    private readonly List<Move> _moveHistory = new();
    private readonly List<ulong> _positionHistory = new(); // Zobrist hashes for repetition detection
    
    public GameState GameState => _gameState;
    public IReadOnlyList<Move> MoveHistory => _moveHistory.AsReadOnly();
    public GameResult Result { get; private set; } = GameResult.Ongoing;
    public DrawReason DrawReason { get; private set; } = DrawReason.None;
    
    public event EventHandler<GameStateChangedEventArgs>? GameStateChanged;
    
    public Game()
    {
        _gameState = new GameState();
        _arbiter = new Arbiter();
        _positionHistory.Add(_gameState.Zobrist);
    }
    
    public Game(string fen) : this()
    {
        _gameState.LoadFromFen(fen);
        _positionHistory.Clear();
        _positionHistory.Add(_gameState.Zobrist);
    }
    
    public List<Move> GetLegalMoves()
    {
        return _arbiter.LegalMoves(_gameState).ToList();
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
        var currentHash = _gameState.Zobrist;
        
        // Execute the move using Arbiter
        var success = _arbiter.TryPlay(_gameState, move);
        if (!success)
            return false;
        
        // Add to history
        _moveHistory.Add(move);
        _positionHistory.Add(_gameState.Zobrist);
        
        // Check game result
        CheckGameResult();
        
        // Notify listeners
        GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(move, Result, DrawReason));
        
        return true;
    }
    
    // Move execution is now handled by the Arbiter
    
    private void CheckGameResult()
    {
        // Use Arbiter to analyze the current position
        Result = _arbiter.GetGameResult(_gameState);
        DrawReason = _arbiter.GetDrawReason(_gameState);
    }
    
    // Check detection and game analysis is now handled by the Arbiter
    
    public void Reset()
    {
        // Reset the game state to starting position
        _gameState.LoadFromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        _moveHistory.Clear();
        _positionHistory.Clear();
        _positionHistory.Add(_gameState.Zobrist);
        Result = GameResult.Ongoing;
        DrawReason = DrawReason.None;
        
        GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(null, Result, DrawReason));
    }
    
    public string GetFen()
    {
        return _gameState.ToFen();
    }
    
    public void LoadFromFen(string fen)
    {
        _gameState.LoadFromFen(fen);
        _moveHistory.Clear();
        _positionHistory.Clear();
        _positionHistory.Add(_gameState.Zobrist);
        Result = GameResult.Ongoing;
        DrawReason = DrawReason.None;
        
        GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(null, Result, DrawReason));
    }
    
    /// <summary>
    /// Try to make a move by coordinates
    /// </summary>
    public Move? TryMakeMove(Square from, Square to, PieceType promotionTo = PieceType.Queen)
    {
        var move = _arbiter.TryPlay(_gameState, from, to, promotionTo);
        if (move != null)
        {
            _moveHistory.Add(move);
            _positionHistory.Add(_gameState.Zobrist);
            CheckGameResult();
            GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(move, Result, DrawReason));
        }
        return move;
    }
    
    /// <summary>
    /// Undo the last move
    /// </summary>
    public bool UndoMove()
    {
        if (_moveHistory.Count == 0)
            return false;
            
        var success = _arbiter.TryUndo(_gameState);
        if (success)
        {
            _moveHistory.RemoveAt(_moveHistory.Count - 1);
            _positionHistory.RemoveAt(_positionHistory.Count - 1);
            CheckGameResult();
            GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(null, Result, DrawReason));
        }
        return success;
    }
    
    /// <summary>
    /// Check if the current side to move is in check
    /// </summary>
    public bool IsInCheck()
    {
        return _arbiter.IsInCheck(_gameState, _gameState.SideToMove);
    }
    
    /// <summary>
    /// Check if the game is over
    /// </summary>
    public bool IsGameOver()
    {
        return _arbiter.IsGameOver(_gameState);
    }
    
    /// <summary>
    /// Get a human-readable description of the current game status
    /// </summary>
    public string GetGameStatus()
    {
        return _arbiter.GetGameStatus(_gameState);
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
