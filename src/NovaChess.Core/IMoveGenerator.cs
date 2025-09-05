namespace NovaChess.Core;

public interface IMoveGenerator
{
    /// <summary>
    /// Generate pseudo-legal moves (may leave king in check)
    /// </summary>
    IEnumerable<Move> GeneratePseudoLegalMoves(GameState state);
    
    /// <summary>
    /// Generate only legal moves (king not left in check)
    /// </summary>
    IEnumerable<Move> GenerateLegalMoves(GameState state);
    
    /// <summary>
    /// Generate legal moves from a specific square
    /// </summary>
    IEnumerable<Move> GenerateLegalMovesFrom(GameState state, Square from);
    
    /// <summary>
    /// Check if the given color is in check
    /// </summary>
    bool IsInCheck(GameState state, Color color);
    
    /// <summary>
    /// Check if a square is attacked by the given color
    /// </summary>
    bool SquareIsAttacked(GameState state, Square square, Color byColor);
    
    /// <summary>
    /// Apply a move to the game state and return undo information
    /// </summary>
    StateDelta ApplyMove(GameState state, Move move);
    
    /// <summary>
    /// Undo a move using the state delta
    /// </summary>
    void UnapplyMove(GameState state, Move move, StateDelta delta);
}
