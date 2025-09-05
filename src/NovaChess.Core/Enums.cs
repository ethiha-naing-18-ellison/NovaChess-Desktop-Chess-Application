namespace NovaChess.Core;

public enum PieceType
{
    None = 0,
    Pawn = 1,
    Knight = 2,
    Bishop = 3,
    Rook = 4,
    Queen = 5,
    King = 6
}

public enum PieceColor
{
    White = 0,
    Black = 1
}

public enum GameResult
{
    Ongoing = 0,
    WhiteWins = 1,
    BlackWins = 2,
    Draw = 3
}

public enum DrawReason
{
    None = 0,
    Stalemate = 1,
    ThreefoldRepetition = 2,
    FiftyMoveRule = 3,
    InsufficientMaterial = 4,
    Agreement = 5
}

public enum CastleRights
{
    None = 0,
    WhiteKingside = 1,
    WhiteQueenside = 2,
    BlackKingside = 4,
    BlackQueenside = 8,
    All = WhiteKingside | WhiteQueenside | BlackKingside | BlackQueenside
}
