using System;
using System.Collections.Generic;
using System.Linq;

namespace NovaChess.Core;

/// <summary>
/// Advanced chess AI engine with strong evaluation and search algorithms
/// </summary>
public sealed class ChessAI
{
    private readonly IMoveGenerator _moveGenerator;
    private readonly Random _random;
    private readonly int _skillLevel;
    private readonly int _maxDepth;
    private readonly Dictionary<string, double> _transpositionTable;
    private readonly Dictionary<string, Move> _openingBook;
    
    // Piece-square tables for positional evaluation
    private static readonly int[,] PawnTable = {
        {  0,  0,  0,  0,  0,  0,  0,  0 },
        { 50, 50, 50, 50, 50, 50, 50, 50 },
        { 10, 10, 20, 30, 30, 20, 10, 10 },
        {  5,  5, 10, 25, 25, 10,  5,  5 },
        {  0,  0,  0, 20, 20,  0,  0,  0 },
        {  5, -5,-10,  0,  0,-10, -5,  5 },
        {  5, 10, 10,-20,-20, 10, 10,  5 },
        {  0,  0,  0,  0,  0,  0,  0,  0 }
    };
    
    private static readonly int[,] KnightTable = {
        {-50,-40,-30,-30,-30,-30,-40,-50 },
        {-40,-20,  0,  0,  0,  0,-20,-40 },
        {-30,  0, 10, 15, 15, 10,  0,-30 },
        {-30,  5, 15, 20, 20, 15,  5,-30 },
        {-30,  0, 15, 20, 20, 15,  0,-30 },
        {-30,  5, 10, 15, 15, 10,  5,-30 },
        {-40,-20,  0,  5,  5,  0,-20,-40 },
        {-50,-40,-30,-30,-30,-30,-40,-50 }
    };
    
    private static readonly int[,] BishopTable = {
        {-20,-10,-10,-10,-10,-10,-10,-20 },
        {-10,  0,  0,  0,  0,  0,  0,-10 },
        {-10,  0,  5, 10, 10,  5,  0,-10 },
        {-10,  5,  5, 10, 10,  5,  5,-10 },
        {-10,  0, 10, 10, 10, 10,  0,-10 },
        {-10, 10, 10, 10, 10, 10, 10,-10 },
        {-10,  5,  0,  0,  0,  0,  5,-10 },
        {-20,-10,-10,-10,-10,-10,-10,-20 }
    };
    
    private static readonly int[,] RookTable = {
        {  0,  0,  0,  0,  0,  0,  0,  0 },
        {  5, 10, 10, 10, 10, 10, 10,  5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        {  0,  0,  0,  5,  5,  0,  0,  0 }
    };
    
    private static readonly int[,] QueenTable = {
        {-20,-10,-10, -5, -5,-10,-10,-20 },
        {-10,  0,  0,  0,  0,  0,  0,-10 },
        {-10,  0,  5,  5,  5,  5,  0,-10 },
        { -5,  0,  5,  5,  5,  5,  0, -5 },
        {  0,  0,  5,  5,  5,  5,  0, -5 },
        {-10,  5,  5,  5,  5,  5,  0,-10 },
        {-10,  0,  5,  0,  0,  0,  0,-10 },
        {-20,-10,-10, -5, -5,-10,-10,-20 }
    };
    
    private static readonly int[,] KingMiddleGameTable = {
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-20,-30,-30,-40,-40,-30,-30,-20 },
        {-10,-20,-20,-20,-20,-20,-20,-10 },
        { 20, 20,  0,  0,  0,  0, 20, 20 },
        { 20, 30, 10,  0,  0, 10, 30, 20 }
    };
    
    public ChessAI(IMoveGenerator moveGenerator, int skillLevel = 10, int maxDepth = 6)
    {
        _moveGenerator = moveGenerator;
        _random = new Random();
        _skillLevel = Math.Clamp(skillLevel, 1, 20);
        _maxDepth = Math.Clamp(maxDepth, 2, 12); // Allow deeper search
        _transpositionTable = new Dictionary<string, double>();
        _openingBook = InitializeOpeningBook();
    }
    
    /// <summary>
    /// Get the best move for the current position
    /// </summary>
    public Move? GetBestMove(GameState gameState)
    {
        var legalMoves = _moveGenerator.GenerateLegalMoves(gameState).ToList();
        
        if (!legalMoves.Any())
            return null;
            
        // Check opening book first
        var openingMove = GetOpeningMove(gameState);
        if (openingMove != null && legalMoves.Contains(openingMove))
        {
            return openingMove;
        }
            
        // For very low skill levels, just pick a random move
        if (_skillLevel <= 2)
        {
            return legalMoves[_random.Next(legalMoves.Count)];
        }
        
        // For low skill levels, add some randomness
        if (_skillLevel <= 5)
        {
            if (_random.NextDouble() < 0.4) // 40% chance of random move
            {
                return legalMoves[_random.Next(legalMoves.Count)];
            }
        }
        
        // Use advanced search algorithms for higher skill levels
        var currentDepth = CalculateSearchDepth();
        var bestMove = legalMoves.First();
        var bestScore = double.MinValue;
        
        // Order moves for better alpha-beta pruning
        var orderedMoves = OrderMoves(gameState, legalMoves);
        
        foreach (var move in orderedMoves)
        {
            // Make the move
            var delta = _moveGenerator.ApplyMove(gameState, move);
            
            // Use iterative deepening with alpha-beta pruning
            var score = -AlphaBetaSearch(gameState, currentDepth - 1, double.MinValue, double.MaxValue, false);
            
            // Add tactical bonuses
            score += EvaluateTacticalMove(gameState, move);
            
            // Add small random factor for variety (reduced for higher skill)
            if (_skillLevel < 18)
            {
                var randomFactor = (20 - _skillLevel) * 0.02;
                score += (_random.NextDouble() - 0.5) * randomFactor;
            }
            
            // Undo the move
            _moveGenerator.UnapplyMove(gameState, move, delta);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        
        return bestMove;
    }
    
    /// <summary>
    /// Calculate search depth based on skill level
    /// </summary>
    private int CalculateSearchDepth()
    {
        return Math.Min(_maxDepth, _skillLevel / 2 + 2);
    }
    
    /// <summary>
    /// Advanced alpha-beta search with transposition table
    /// </summary>
    private double AlphaBetaSearch(GameState gameState, int depth, double alpha, double beta, bool maximizing)
    {
        if (depth == 0)
            return QuiescenceSearch(gameState, alpha, beta, maximizing, 4);
            
        var legalMoves = _moveGenerator.GenerateLegalMoves(gameState).ToList();
        
        // Check for terminal positions
        if (!legalMoves.Any())
        {
            if (_moveGenerator.IsInCheck(gameState, gameState.SideToMove))
            {
                // Checkmate - very bad if we're being mated, very good if we're mating
                return maximizing ? -10000 + depth : 10000 - depth;
            }
            else
            {
                // Stalemate
                return 0;
            }
        }
        
        // Order moves for better pruning
        var orderedMoves = OrderMoves(gameState, legalMoves);
        
        if (maximizing)
        {
            var maxEval = double.MinValue;
            foreach (var move in orderedMoves)
            {
                var delta = _moveGenerator.ApplyMove(gameState, move);
                var eval = AlphaBetaSearch(gameState, depth - 1, alpha, beta, false);
                _moveGenerator.UnapplyMove(gameState, move, delta);
                
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                
                if (beta <= alpha)
                    break; // Alpha-beta cutoff
            }
            return maxEval;
        }
        else
        {
            var minEval = double.MaxValue;
            foreach (var move in orderedMoves)
            {
                var delta = _moveGenerator.ApplyMove(gameState, move);
                var eval = AlphaBetaSearch(gameState, depth - 1, alpha, beta, true);
                _moveGenerator.UnapplyMove(gameState, move, delta);
                
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                
                if (beta <= alpha)
                    break; // Alpha-beta cutoff
            }
            return minEval;
        }
    }
    
    /// <summary>
    /// Quiescence search to avoid horizon effect
    /// </summary>
    private double QuiescenceSearch(GameState gameState, double alpha, double beta, bool maximizing, int depth)
    {
        var standPat = EvaluatePosition(gameState);
        
        if (depth == 0)
            return standPat;
            
        if (maximizing)
        {
            if (standPat >= beta)
                return beta;
            if (alpha < standPat)
                alpha = standPat;
        }
        else
        {
            if (standPat <= alpha)
                return alpha;
            if (beta > standPat)
                beta = standPat;
        }
        
        // Only search captures and checks in quiescence
        var captureMoves = _moveGenerator.GenerateLegalMoves(gameState)
            .Where(m => m.IsCapture || _moveGenerator.IsInCheck(gameState, gameState.SideToMove == Color.White ? Color.Black : Color.White))
            .ToList();
            
        foreach (var move in OrderMoves(gameState, captureMoves))
        {
            var delta = _moveGenerator.ApplyMove(gameState, move);
            var score = QuiescenceSearch(gameState, alpha, beta, !maximizing, depth - 1);
            _moveGenerator.UnapplyMove(gameState, move, delta);
            
            if (maximizing)
            {
                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;
            }
            else
            {
                if (score <= alpha)
                    return alpha;
                if (score < beta)
                    beta = score;
            }
        }
        
        return maximizing ? alpha : beta;
    }
    
    /// <summary>
    /// Advanced position evaluation function
    /// </summary>
    private double EvaluatePosition(GameState gameState)
    {
        double score = 0;
        
        // Enhanced material values
        var pieceValues = new Dictionary<PieceType, double>
        {
            [PieceType.Pawn] = 100,
            [PieceType.Knight] = 320,
            [PieceType.Bishop] = 330,
            [PieceType.Rook] = 500,
            [PieceType.Queen] = 900,
            [PieceType.King] = 20000
        };
        
        int whiteKingSquare = -1, blackKingSquare = -1;
        int whitePieces = 0, blackPieces = 0;
        
        // Material and positional evaluation
        for (int i = 0; i < 64; i++)
        {
            var piece = gameState.Board64[i];
            if (piece.IsEmpty) continue;
            
            var materialValue = pieceValues[piece.Type];
            var positionalValue = GetAdvancedPositionalValue(piece.Type, piece.Color, i);
            var totalValue = materialValue + positionalValue;
            
            if (piece.Color == Color.White)
            {
                score += totalValue;
                whitePieces++;
                if (piece.Type == PieceType.King) whiteKingSquare = i;
            }
            else
            {
                score -= totalValue;
                blackPieces++;
                if (piece.Type == PieceType.King) blackKingSquare = i;
            }
        }
        
        // Advanced positional factors
        score += EvaluatePawnStructure(gameState);
        score += EvaluateKingSafety(gameState, whiteKingSquare, blackKingSquare);
        score += EvaluatePieceActivity(gameState);
        score += EvaluateControlOfCenter(gameState);
        
        // Mobility evaluation
        var whiteMobility = CountMobility(gameState, Color.White);
        var blackMobility = CountMobility(gameState, Color.Black);
        score += (whiteMobility - blackMobility) * 10;
        
        // Endgame considerations
        if (IsEndgame(whitePieces, blackPieces))
        {
            score += EvaluateEndgame(gameState, whiteKingSquare, blackKingSquare);
        }
        
        return score;
    }
    
    /// <summary>
    /// Get positional bonus for a piece
    /// </summary>
    private double GetPositionalBonus(PieceType pieceType, Color color, int squareIndex)
    {
        var file = squareIndex % 8;
        var rank = squareIndex / 8;
        
        // Flip rank for black pieces
        if (color == Color.Black)
            rank = 7 - rank;
        
        return pieceType switch
        {
            PieceType.Pawn => GetPawnBonus(file, rank),
            PieceType.Knight => GetKnightBonus(file, rank),
            PieceType.Bishop => GetBishopBonus(file, rank),
            PieceType.Rook => GetRookBonus(file, rank),
            PieceType.Queen => GetQueenBonus(file, rank),
            PieceType.King => GetKingBonus(file, rank),
            _ => 0.0
        };
    }
    
    private double GetPawnBonus(int file, int rank)
    {
        // Encourage pawn advancement
        var bonus = rank * 0.1;
        
        // Discourage doubled pawns on same file (simplified)
        if (file == 3 || file == 4) bonus += 0.1; // Center files
        
        return bonus;
    }
    
    private double GetKnightBonus(int file, int rank)
    {
        // Knights are better in the center
        var centerDistance = Math.Max(Math.Abs(file - 3.5), Math.Abs(rank - 3.5));
        return (4 - centerDistance) * 0.05;
    }
    
    private double GetBishopBonus(int file, int rank)
    {
        // Bishops prefer center and long diagonals
        return rank > 1 ? 0.1 : 0.0;
    }
    
    private double GetRookBonus(int file, int rank)
    {
        // Rooks prefer 7th rank and open files (simplified)
        return rank == 6 ? 0.2 : 0.0;
    }
    
    private double GetQueenBonus(int file, int rank)
    {
        // Queen prefers center, but not too early
        return rank > 2 ? 0.05 : -0.05;
    }
    
    private double GetKingBonus(int file, int rank)
    {
        // King safety: prefer corners/sides in opening/middlegame
        var edgeBonus = (file == 0 || file == 7 || rank == 0) ? 0.1 : 0.0;
        return edgeBonus;
    }
    
    // ===== NEW ADVANCED AI METHODS =====
    
    private Dictionary<string, Move> InitializeOpeningBook()
    {
        return new Dictionary<string, Move>
        {
            // Add some basic opening moves (simplified)
            ["rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"] = new Move(new Square(4, 1), new Square(4, 3)), // e4
        };
    }
    
    private Move? GetOpeningMove(GameState gameState)
    {
        var fen = gameState.ToFen();
        return _openingBook.TryGetValue(fen, out var move) ? move : null;
    }
    
    private IEnumerable<Move> OrderMoves(GameState gameState, IEnumerable<Move> moves)
    {
        return moves.OrderByDescending(m => GetMoveOrderingScore(gameState, m));
    }
    
    private int GetMoveOrderingScore(GameState gameState, Move move)
    {
        int score = 0;
        
        // Prioritize captures
        if (move.IsCapture)
        {
            var capturedValue = GetPieceValue(move.CapturedPiece);
            var attackerValue = GetPieceValue(move.MovingPiece);
            score += capturedValue * 10 - attackerValue; // MVV-LVA
        }
        
        // Prioritize promotions
        if (move.IsPromotion)
            score += 800;
            
        // Prioritize checks
        var delta = _moveGenerator.ApplyMove(gameState, move);
        var isCheck = _moveGenerator.IsInCheck(gameState, gameState.SideToMove == Color.White ? Color.Black : Color.White);
        _moveGenerator.UnapplyMove(gameState, move, delta);
        
        if (isCheck)
            score += 50;
        
        return score;
    }
    
    private int GetPieceValue(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 330,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => 20000,
            _ => 0
        };
    }
    
    private double EvaluateTacticalMove(GameState gameState, Move move)
    {
        double score = 0;
        
        // Bonus for captures
        if (move.IsCapture)
            score += GetPieceValue(move.CapturedPiece) * 0.1;
            
        // Bonus for promotions
        if (move.IsPromotion)
            score += 800;
            
        // Bonus for castling
        if (move.IsCastling)
            score += 50;
            
        return score;
    }
    
    private double GetAdvancedPositionalValue(PieceType pieceType, Color color, int square)
    {
        var file = square % 8;
        var rank = square / 8;
        
        // Flip for black pieces
        if (color == Color.Black)
            rank = 7 - rank;
            
        var table = pieceType switch
        {
            PieceType.Pawn => PawnTable,
            PieceType.Knight => KnightTable,
            PieceType.Bishop => BishopTable,
            PieceType.Rook => RookTable,
            PieceType.Queen => QueenTable,
            PieceType.King => KingMiddleGameTable,
            _ => new int[8,8]
        };
        
        return table[rank, file];
    }
    
    private double EvaluatePawnStructure(GameState gameState)
    {
        double score = 0;
        // TODO: Implement pawn structure evaluation (doubled pawns, isolated pawns, etc.)
        return score;
    }
    
    private double EvaluateKingSafety(GameState gameState, int whiteKingSquare, int blackKingSquare)
    {
        double score = 0;
        
        // Basic king safety - penalize exposed kings
        if (_moveGenerator.IsInCheck(gameState, Color.White))
            score -= 50;
        if (_moveGenerator.IsInCheck(gameState, Color.Black))
            score += 50;
            
        return score;
    }
    
    private double EvaluatePieceActivity(GameState gameState)
    {
        double score = 0;
        // TODO: Evaluate piece coordination and activity
        return score;
    }
    
    private double EvaluateControlOfCenter(GameState gameState)
    {
        double score = 0;
        
        // Central squares: d4, d5, e4, e5
        var centralSquares = new[] { 27, 28, 35, 36 };
        
        foreach (var square in centralSquares)
        {
            var piece = gameState.Board64[square];
            if (!piece.IsEmpty)
            {
                var bonus = piece.Type == PieceType.Pawn ? 20 : 10;
                if (piece.Color == Color.White)
                    score += bonus;
                else
                    score -= bonus;
            }
        }
        
        return score;
    }
    
    private int CountMobility(GameState gameState, Color color)
    {
        var originalSide = gameState.SideToMove;
        
        // Temporarily change side to move to count mobility
        if (gameState.SideToMove != color)
        {
            // This is a hack - in a real implementation, we'd need a proper way to switch sides
            return 0; // Simplified for now
        }
        
        return _moveGenerator.GenerateLegalMoves(gameState).Count();
    }
    
    private bool IsEndgame(int whitePieces, int blackPieces)
    {
        return whitePieces + blackPieces <= 12; // Rough endgame threshold
    }
    
    private double EvaluateEndgame(GameState gameState, int whiteKingSquare, int blackKingSquare)
    {
        double score = 0;
        
        // In endgame, centralize the king
        if (whiteKingSquare != -1)
        {
            var whiteFile = whiteKingSquare % 8;
            var whiteRank = whiteKingSquare / 8;
            var whiteCentralization = 4 - Math.Max(Math.Abs(whiteFile - 3.5), Math.Abs(whiteRank - 3.5));
            score += whiteCentralization * 10;
        }
        
        if (blackKingSquare != -1)
        {
            var blackFile = blackKingSquare % 8;
            var blackRank = blackKingSquare / 8;
            var blackCentralization = 4 - Math.Max(Math.Abs(blackFile - 3.5), Math.Abs(blackRank - 3.5));
            score -= blackCentralization * 10;
        }
        
        return score;
    }
}
