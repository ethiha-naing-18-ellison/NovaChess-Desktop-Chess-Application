using NovaChess.Core;

namespace NovaChess.Infrastructure.Interfaces;

public interface IEngineService : IDisposable
{
    bool IsConnected { get; }
    string EngineName { get; }
    string EngineAuthor { get; }
    
    Task<bool> ConnectAsync(string enginePath);
    Task DisconnectAsync();
    
    Task<bool> SetOptionAsync(string name, string value);
    Task<bool> SetSkillLevelAsync(int level);
    Task<bool> SetDepthAsync(int depth);
    Task<bool> SetMoveTimeAsync(int milliseconds);
    
    Task<EngineEvaluation?> GetBestMoveAsync(string fen, int depth = 20);
    Task<EngineEvaluation?> GetBestMoveAsync(string fen, TimeSpan timeLimit);
    
    event EventHandler<EngineInfoEventArgs>? EngineInfoReceived;
}

public class EngineEvaluation
{
    public string? BestMove { get; set; } // UCI move string like "e2e4"
    public int Score { get; set; } // Centipawns
    public bool IsMate { get; set; }
    public int MateIn { get; set; }
    public List<string> PrincipalVariation { get; set; } = new(); // UCI move strings
    public int Depth { get; set; }
    public TimeSpan TimeUsed { get; set; }
}

public class EngineInfoEventArgs : EventArgs
{
    public string Info { get; }
    public EngineEvaluation? Evaluation { get; }
    
    public EngineInfoEventArgs(string info, EngineEvaluation? evaluation = null)
    {
        Info = info;
        Evaluation = evaluation;
    }
}
