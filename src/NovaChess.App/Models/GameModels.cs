namespace NovaChess.App.Models;

public class GameConfiguration
{
    public GameMode GameMode { get; set; }
    public GameTimeControl TimeControl { get; set; }
    public string StartingPosition { get; set; } = "";
    public int AISkillLevel { get; set; } = 10;
    public int AISearchDepth { get; set; } = 15;
}

public enum GameMode
{
    PlayerVsPlayer,
    PlayerVsComputer
}

public enum GameTimeControl
{
    NoLimit,
    Blitz,
    Rapid,
    Classical,
    Custom
}

public class MoveListItem
{
    public int MoveNumber { get; set; }
    public string WhiteMove { get; set; } = "";
    public string BlackMove { get; set; } = "";
}
