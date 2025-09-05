namespace NovaChess.Core;

public class PgnParser
{
    public static PgnGame Parse(string pgn)
    {
        var game = new PgnGame();
        var lines = pgn.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Parse tags
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                var tagContent = trimmedLine.Substring(1, trimmedLine.Length - 2);
                var colonIndex = tagContent.IndexOf('"');
                if (colonIndex > 0)
                {
                    var tagName = tagContent.Substring(0, colonIndex).Trim();
                    var tagValue = tagContent.Substring(colonIndex + 1);
                    if (tagValue.EndsWith('"'))
                        tagValue = tagValue.Substring(0, tagValue.Length - 1);
                    
                    game.Tags[tagName] = tagValue;
                }
            }
        }
        
        // Parse moves
        var moveText = string.Join(" ", lines.Where(l => !l.Trim().StartsWith('[')));
        ParseMoves(game, moveText);
        
        return game;
    }
    
    private static void ParseMoves(PgnGame game, string moveText)
    {
        // Remove move numbers and comments
        var cleanText = System.Text.RegularExpressions.Regex.Replace(moveText, @"\d+\.", "");
        cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\{[^}]*\}", "");
        cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @";.*", "");
        
        var moves = cleanText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var move in moves)
        {
            var trimmedMove = move.Trim();
            if (string.IsNullOrEmpty(trimmedMove) || trimmedMove == "*")
                continue;
                
            if (trimmedMove == "1-0")
            {
                game.Result = GameResult.WhiteWins;
            }
            else if (trimmedMove == "0-1")
            {
                game.Result = GameResult.BlackWins;
            }
            else if (trimmedMove == "1/2-1/2")
            {
                game.Result = GameResult.Draw;
            }
            else
            {
                var pgnMove = ParseMove(trimmedMove);
                if (pgnMove != null)
                    game.Moves.Add(pgnMove);
            }
        }
    }
    
    private static PgnMove? ParseMove(string moveText)
    {
        // This is a simplified move parser
        // In production, you'd want to handle all the edge cases
        
        if (moveText == "O-O" || moveText == "0-0")
            return new PgnMove { IsCastle = true, CastleType = CastleType.Kingside };
            
        if (moveText == "O-O-O" || moveText == "0-0-0")
            return new PgnMove { IsCastle = true, CastleType = CastleType.Queenside };
            
        // Handle regular moves
        var move = new PgnMove();
        
        // Check for promotion
        var promotionIndex = moveText.IndexOf('=');
        if (promotionIndex > 0)
        {
            var promotionChar = moveText[promotionIndex + 1];
            move.PromotionPiece = promotionChar switch
            {
                'Q' => PieceType.Queen,
                'R' => PieceType.Rook,
                'B' => PieceType.Bishop,
                'N' => PieceType.Knight,
                _ => PieceType.None
            };
            moveText = moveText.Substring(0, promotionIndex);
        }
        
        // Check for check/checkmate
        if (moveText.EndsWith('#'))
        {
            move.IsCheckmate = true;
            moveText = moveText.Substring(0, moveText.Length - 1);
        }
        else if (moveText.EndsWith('+'))
        {
            move.IsCheck = true;
            moveText = moveText.Substring(0, moveText.Length - 1);
        }
        
        // Check for capture
        if (moveText.Contains('x'))
        {
            move.IsCapture = true;
            var parts = moveText.Split('x');
            if (parts.Length == 2)
            {
                move.Destination = ParseSquare(parts[1]);
                moveText = parts[0];
            }
        }
        else
        {
            // Try to find destination square
            var lastTwoChars = moveText.Length >= 2 ? moveText.Substring(moveText.Length - 2) : "";
            if (IsValidSquare(lastTwoChars))
            {
                move.Destination = ParseSquare(lastTwoChars);
                moveText = moveText.Substring(0, moveText.Length - 2);
            }
        }
        
        // Determine piece type
        if (moveText.Length > 0)
        {
            var firstChar = moveText[0];
            if (char.IsUpper(firstChar))
            {
                move.Piece = firstChar switch
                {
                    'N' => PieceType.Knight,
                    'B' => PieceType.Bishop,
                    'R' => PieceType.Rook,
                    'Q' => PieceType.Queen,
                    'K' => PieceType.King,
                    _ => PieceType.Pawn
                };
                moveText = moveText.Substring(1);
            }
            else
            {
                move.Piece = PieceType.Pawn;
            }
        }
        
        // Handle disambiguation
        if (moveText.Length > 0)
        {
            if (char.IsLetter(moveText[0]))
            {
                move.DisambiguationFile = moveText[0] - 'a';
                moveText = moveText.Substring(1);
            }
            else if (char.IsDigit(moveText[0]))
            {
                move.DisambiguationRank = moveText[0] - '1';
                moveText = moveText.Substring(1);
            }
        }
        
        return move;
    }
    
    private static Square ParseSquare(string squareText)
    {
        if (squareText.Length == 2)
        {
            var file = char.ToLower(squareText[0]) - 'a';
            var rank = squareText[1] - '1';
            return new Square(file, rank);
        }
        return Square.None;
    }
    
    private static bool IsValidSquare(string text)
    {
        if (text.Length != 2) return false;
        var file = char.ToLower(text[0]) - 'a';
        var rank = text[1] - '1';
        return file >= 0 && file < 8 && rank >= 0 && rank < 8;
    }
    
    public static string ToPgn(PgnGame game)
    {
        var pgn = new System.Text.StringBuilder();
        
        // Write tags
        foreach (var tag in game.Tags)
        {
            pgn.AppendLine($"[{tag.Key} \"{tag.Value}\"]");
        }
        
        pgn.AppendLine();
        
        // Write moves
        for (int i = 0; i < game.Moves.Count; i++)
        {
            if (i % 2 == 0)
            {
                pgn.Append($"{(i / 2) + 1}.");
            }
            
            pgn.Append($" {MoveToSan(game.Moves[i])}");
            
            if (i % 2 == 1)
                pgn.AppendLine();
        }
        
        // Write result
        pgn.Append(" ");
        pgn.Append(game.Result switch
        {
            GameResult.WhiteWins => "1-0",
            GameResult.BlackWins => "0-1",
            GameResult.Draw => "1/2-1/2",
            _ => "*"
        });
        
        return pgn.ToString();
    }
    
    private static string MoveToSan(PgnMove move)
    {
        if (move.IsCastle)
        {
            return move.CastleType == CastleType.Kingside ? "O-O" : "O-O-O";
        }
        
        var san = "";
        
        // Piece letter (except for pawns)
        if (move.Piece != PieceType.Pawn)
        {
            san += move.Piece switch
            {
                PieceType.Knight => "N",
                PieceType.Bishop => "B",
                PieceType.Rook => "R",
                PieceType.Queen => "Q",
                PieceType.King => "K",
                _ => ""
            };
        }
        
        // Disambiguation
        if (move.DisambiguationFile >= 0)
            san += (char)('a' + move.DisambiguationFile);
        if (move.DisambiguationRank >= 0)
            san += (char)('1' + move.DisambiguationRank);
        
        // Capture
        if (move.IsCapture)
            san += "x";
        
        // Destination
        if (move.Destination.IsValid)
            san += move.Destination.ToAlgebraic();
        
        // Promotion
        if (move.PromotionPiece != PieceType.None)
        {
            san += "=" + move.PromotionPiece switch
            {
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                _ => ""
            };
        }
        
        // Check/Checkmate
        if (move.IsCheckmate)
            san += "#";
        else if (move.IsCheck)
            san += "+";
            
        return san;
    }
}

public class PgnGame
{
    public Dictionary<string, string> Tags { get; } = new();
    public List<PgnMove> Moves { get; } = new();
    public GameResult Result { get; set; } = GameResult.Ongoing;
}

public class PgnMove
{
    public PieceType Piece { get; set; } = PieceType.Pawn;
    public Square Destination { get; set; } = Square.None;
    public PieceType PromotionPiece { get; set; } = PieceType.None;
    public bool IsCapture { get; set; }
    public bool IsCheck { get; set; }
    public bool IsCheckmate { get; set; }
    public bool IsCastle { get; set; }
    public CastleType CastleType { get; set; }
    public int DisambiguationFile { get; set; } = -1;
    public int DisambiguationRank { get; set; } = -1;
}

public enum CastleType
{
    Kingside,
    Queenside
}
