using System.Diagnostics;
using System.Text.RegularExpressions;
using NovaChess.Core;
using NovaChess.Infrastructure.Interfaces;

namespace NovaChess.Infrastructure.Services;

public class EngineService : IEngineService
{
    private Process? _engineProcess;
    private readonly object _lockObject = new();
    private readonly List<string> _engineOptions = new();
    
    public bool IsConnected { get; private set; }
    public string EngineName { get; private set; } = "";
    public string EngineAuthor { get; private set; } = "";
    
    public event EventHandler<EngineInfoEventArgs>? EngineInfoReceived;
    
    public async Task<bool> ConnectAsync(string enginePath)
    {
        if (IsConnected)
            await DisconnectAsync();
            
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = enginePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            _engineProcess = new Process { StartInfo = startInfo };
            _engineProcess.Start();
            
            // Wait for engine to start
            await Task.Delay(100);
            
            // Send UCI command
            await SendCommandAsync("uci");
            
            // Wait for uciok
            var uciOkReceived = false;
            var timeout = DateTime.UtcNow.AddSeconds(5);
            
            while (!uciOkReceived && DateTime.UtcNow < timeout)
            {
                var line = await _engineProcess.StandardOutput.ReadLineAsync();
                if (line != null)
                {
                    ProcessEngineOutput(line);
                    if (line == "uciok")
                        uciOkReceived = true;
                }
            }
            
            if (!uciOkReceived)
            {
                await DisconnectAsync();
                return false;
            }
            
            // Send isready
            await SendCommandAsync("isready");
            
            // Wait for readyok
            var readyOkReceived = false;
            timeout = DateTime.UtcNow.AddSeconds(5);
            
            while (!readyOkReceived && DateTime.UtcNow < timeout)
            {
                var line = await _engineProcess.StandardOutput.ReadLineAsync();
                if (line != null)
                {
                    ProcessEngineOutput(line);
                    if (line == "readyok")
                        readyOkReceived = true;
                }
            }
            
            if (!readyOkReceived)
            {
                await DisconnectAsync();
                return false;
            }
            
            IsConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to connect to engine: {ex.Message}");
            await DisconnectAsync();
            return false;
        }
    }
    
    public async Task DisconnectAsync()
    {
        if (_engineProcess != null)
        {
            try
            {
                if (!_engineProcess.HasExited)
                {
                    await SendCommandAsync("quit");
                    await Task.Delay(100);
                    
                    if (!_engineProcess.HasExited)
                        _engineProcess.Kill();
                }
            }
            catch
            {
                // Ignore errors during shutdown
            }
            finally
            {
                _engineProcess?.Dispose();
                _engineProcess = null;
            }
        }
        
        IsConnected = false;
        EngineName = "";
        EngineAuthor = "";
    }
    
    public async Task<bool> SetOptionAsync(string name, string value)
    {
        if (!IsConnected || _engineProcess == null)
            return false;
            
        try
        {
            await SendCommandAsync($"setoption name {name} value {value}");
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> SetSkillLevelAsync(int level)
    {
        if (level < 0 || level > 20)
            return false;
            
        return await SetOptionAsync("Skill Level", level.ToString());
    }
    
    public async Task<bool> SetDepthAsync(int depth)
    {
        if (depth < 1 || depth > 50)
            return false;
            
        return await SetOptionAsync("MultiPV", "1");
    }
    
    public async Task<bool> SetMoveTimeAsync(int milliseconds)
    {
        if (milliseconds < 1)
            return false;
            
        return await SetOptionAsync("Move Overhead", milliseconds.ToString());
    }
    
    public async Task<EngineEvaluation?> GetBestMoveAsync(string fen, int depth = 20)
    {
        if (!IsConnected || _engineProcess == null)
            return null;
            
        try
        {
            // Set position
            await SendCommandAsync($"position fen {fen}");
            
            // Start analysis
            await SendCommandAsync($"go depth {depth}");
            
            // Wait for bestmove
            var bestMoveReceived = false;
            var evaluation = new EngineEvaluation();
            var timeout = DateTime.UtcNow.AddSeconds(30);
            
            while (!bestMoveReceived && DateTime.UtcNow < timeout)
            {
                var line = await _engineProcess.StandardOutput.ReadLineAsync();
                if (line != null)
                {
                    if (line.StartsWith("bestmove"))
                    {
                        bestMoveReceived = true;
                        ParseBestMove(line, evaluation);
                    }
                    else
                    {
                        ProcessEngineOutput(line);
                        ParseInfo(line, evaluation);
                    }
                }
            }
            
            return bestMoveReceived ? evaluation : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get best move: {ex.Message}");
            return null;
        }
    }
    
    public async Task<EngineEvaluation?> GetBestMoveAsync(string fen, TimeSpan timeLimit)
    {
        if (!IsConnected || _engineProcess == null)
            return null;
            
        try
        {
            // Set position
            await SendCommandAsync($"position fen {fen}");
            
            // Start analysis with time limit
            var moveTime = (int)timeLimit.TotalMilliseconds;
            await SendCommandAsync($"go movetime {moveTime}");
            
            // Wait for bestmove
            var bestMoveReceived = false;
            var evaluation = new EngineEvaluation();
            var timeout = DateTime.UtcNow.AddSeconds(timeLimit.TotalSeconds + 5);
            
            while (!bestMoveReceived && DateTime.UtcNow < timeout)
            {
                var line = await _engineProcess.StandardOutput.ReadLineAsync();
                if (line != null)
                {
                    if (line.StartsWith("bestmove"))
                    {
                        bestMoveReceived = true;
                        ParseBestMove(line, evaluation);
                    }
                    else
                    {
                        ProcessEngineOutput(line);
                        ParseInfo(line, evaluation);
                    }
                }
            }
            
            return bestMoveReceived ? evaluation : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get best move: {ex.Message}");
            return null;
        }
    }
    
    private async Task SendCommandAsync(string command)
    {
        if (_engineProcess?.StandardInput != null)
        {
            await _engineProcess.StandardInput.WriteLineAsync(command);
            await _engineProcess.StandardInput.FlushAsync();
        }
    }
    
    private void ProcessEngineOutput(string line)
    {
        if (line.StartsWith("id name"))
        {
            EngineName = line.Substring(8).Trim();
        }
        else if (line.StartsWith("id author"))
        {
            EngineAuthor = line.Substring(11).Trim();
        }
        else if (line.StartsWith("option"))
        {
            _engineOptions.Add(line);
        }
        
        EngineInfoReceived?.Invoke(this, new EngineInfoEventArgs(line));
    }
    
    private void ParseBestMove(string line, EngineEvaluation evaluation)
    {
        var parts = line.Split(' ');
        if (parts.Length >= 2)
        {
            var moveText = parts[1];
            if (moveText != "(none)")
            {
                // Parse UCI move format (e2e4, e7e8q, etc.)
                if (moveText.Length >= 4)
                {
                    var fromSquare = Square.FromAlgebraic(moveText.Substring(0, 2));
                    var toSquare = Square.FromAlgebraic(moveText.Substring(2, 2));
                    
                    // Determine piece type and promotion
                    PieceType promotionPiece = PieceType.None;
                    if (moveText.Length == 5)
                    {
                        promotionPiece = moveText[4] switch
                        {
                            'q' => PieceType.Queen,
                            'r' => PieceType.Rook,
                            'b' => PieceType.Bishop,
                            'n' => PieceType.Knight,
                            _ => PieceType.None
                        };
                    }
                    
                    evaluation.BestMove = moveText;
                }
            }
        }
    }
    
    private void ParseInfo(string line, EngineEvaluation evaluation)
    {
        if (line.StartsWith("info"))
        {
            // Parse score
            var scoreMatch = Regex.Match(line, @"score (cp|mate) (-?\d+)");
            if (scoreMatch.Success)
            {
                var scoreType = scoreMatch.Groups[1].Value;
                var scoreValue = int.Parse(scoreMatch.Groups[2].Value);
                
                if (scoreType == "cp")
                {
                    evaluation.Score = scoreValue;
                    evaluation.IsMate = false;
                }
                else if (scoreType == "mate")
                {
                    evaluation.IsMate = true;
                    evaluation.MateIn = scoreValue;
                }
            }
            
            // Parse depth
            var depthMatch = Regex.Match(line, @"depth (\d+)");
            if (depthMatch.Success)
            {
                evaluation.Depth = int.Parse(depthMatch.Groups[1].Value);
            }
            
            // Parse time
            var timeMatch = Regex.Match(line, @"time (\d+)");
            if (timeMatch.Success)
            {
                evaluation.TimeUsed = TimeSpan.FromMilliseconds(int.Parse(timeMatch.Groups[1].Value));
            }
            
            // Parse principal variation
            var pvMatch = Regex.Match(line, @"pv (.+)");
            if (pvMatch.Success)
            {
                var pvMoves = pvMatch.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                evaluation.PrincipalVariation.Clear();
                
                foreach (var moveText in pvMoves)
                {
                    if (moveText.Length >= 4)
                    {
                        var fromSquare = Square.FromAlgebraic(moveText.Substring(0, 2));
                        var toSquare = Square.FromAlgebraic(moveText.Substring(2, 2));
                        
                        PieceType promotionPiece = PieceType.None;
                        if (moveText.Length == 5)
                        {
                            promotionPiece = moveText[4] switch
                            {
                                'q' => PieceType.Queen,
                                'r' => PieceType.Rook,
                                'b' => PieceType.Bishop,
                                'n' => PieceType.Knight,
                                _ => PieceType.None
                            };
                        }
                        
                        evaluation.PrincipalVariation.Add(moveText);
                    }
                }
            }
        }
    }
    
    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}
