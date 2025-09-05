using System.Timers;

namespace NovaChess.Core;

public class Clock : IDisposable
{
    private readonly System.Timers.Timer _timer;
    private readonly object _lockObject = new();
    
    public TimeSpan WhiteTime { get; private set; }
    public TimeSpan BlackTime { get; private set; }
    public TimeSpan Increment { get; }
    public bool IsRunning { get; private set; }
    public PieceColor ActivePlayer { get; private set; }
    public bool IsPaused { get; private set; }
    
    public event EventHandler<ClockEventArgs>? TimeChanged;
    public event EventHandler<ClockEventArgs>? TimeExpired;
    
    public Clock(TimeSpan initialTime, TimeSpan increment)
    {
        WhiteTime = initialTime;
        BlackTime = initialTime;
        Increment = increment;
        ActivePlayer = PieceColor.White;
        
        _timer = new System.Timers.Timer(100); // Update every 100ms
        _timer.Elapsed += OnTimerElapsed;
    }
    
    public void Start()
    {
        lock (_lockObject)
        {
            if (!IsRunning && !IsPaused)
            {
                IsRunning = true;
                _timer.Start();
            }
        }
    }
    
    public void Stop()
    {
        lock (_lockObject)
        {
            IsRunning = false;
            _timer.Stop();
        }
    }
    
    public void Pause()
    {
        lock (_lockObject)
        {
            if (IsRunning)
            {
                IsPaused = true;
                _timer.Stop();
            }
        }
    }
    
    public void Resume()
    {
        lock (_lockObject)
        {
            if (IsPaused)
            {
                IsPaused = false;
                IsRunning = true;
                _timer.Start();
            }
        }
    }
    
    public void SwitchPlayer()
    {
        lock (_lockObject)
        {
            if (IsRunning)
            {
                // Add increment to the player who just moved
                if (ActivePlayer == PieceColor.White)
                {
                    WhiteTime += Increment;
                    ActivePlayer = PieceColor.Black;
                }
                else
                {
                    BlackTime += Increment;
                    ActivePlayer = PieceColor.White;
                }
                
                TimeChanged?.Invoke(this, new ClockEventArgs(WhiteTime, BlackTime, ActivePlayer));
            }
        }
    }
    
    public void AddTime(PieceColor player, TimeSpan time)
    {
        lock (_lockObject)
        {
            if (player == PieceColor.White)
                WhiteTime += time;
            else
                BlackTime += time;
                
            TimeChanged?.Invoke(this, new ClockEventArgs(WhiteTime, BlackTime, ActivePlayer));
        }
    }
    
    public void SetTime(PieceColor player, TimeSpan time)
    {
        lock (_lockObject)
        {
            if (player == PieceColor.White)
                WhiteTime = time;
            else
                BlackTime = time;
                
            TimeChanged?.Invoke(this, new ClockEventArgs(WhiteTime, BlackTime, ActivePlayer));
        }
    }
    
    public void Reset(TimeSpan whiteTime, TimeSpan blackTime)
    {
        lock (_lockObject)
        {
            WhiteTime = whiteTime;
            BlackTime = blackTime;
            ActivePlayer = PieceColor.White;
            IsRunning = false;
            IsPaused = false;
            _timer.Stop();
            
            TimeChanged?.Invoke(this, new ClockEventArgs(WhiteTime, BlackTime, ActivePlayer));
        }
    }
    
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lockObject)
        {
            if (!IsRunning || IsPaused)
                return;
                
            if (ActivePlayer == PieceColor.White)
            {
                if (WhiteTime > TimeSpan.Zero)
                {
                    WhiteTime = WhiteTime.Subtract(TimeSpan.FromMilliseconds(100));
                    if (WhiteTime < TimeSpan.Zero)
                        WhiteTime = TimeSpan.Zero;
                        
                    TimeChanged?.Invoke(this, new ClockEventArgs(WhiteTime, BlackTime, ActivePlayer));
                    
                    if (WhiteTime == TimeSpan.Zero)
                    {
                        TimeExpired?.Invoke(this, new ClockEventArgs(WhiteTime, BlackTime, ActivePlayer));
                        Stop();
                    }
                }
            }
            else
            {
                if (BlackTime > TimeSpan.Zero)
                {
                    BlackTime = BlackTime.Subtract(TimeSpan.FromMilliseconds(100));
                    if (BlackTime < TimeSpan.Zero)
                        BlackTime = TimeSpan.Zero;
                        
                    TimeChanged?.Invoke(this, new ClockEventArgs(WhiteTime, BlackTime, ActivePlayer));
                    
                    if (BlackTime == TimeSpan.Zero)
                    {
                        TimeExpired?.Invoke(this, new ClockEventArgs(WhiteTime, BlackTime, ActivePlayer));
                        Stop();
                    }
                }
            }
        }
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
    }
}

public class ClockEventArgs : EventArgs
{
    public TimeSpan WhiteTime { get; }
    public TimeSpan BlackTime { get; }
    public PieceColor ActivePlayer { get; }
    
    public ClockEventArgs(TimeSpan whiteTime, TimeSpan blackTime, PieceColor activePlayer)
    {
        WhiteTime = whiteTime;
        BlackTime = blackTime;
        ActivePlayer = activePlayer;
    }
}

public static class TimeControl
{
    public static readonly TimeSpan Bullet = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan Blitz = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan Rapid = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan Classical = TimeSpan.FromMinutes(15);
    
    public static readonly TimeSpan StandardIncrement = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan NoIncrement = TimeSpan.Zero;
    
    public static Clock CreateClock(string timeControl)
    {
        var parts = timeControl.Split('+');
        var baseTime = ParseTime(parts[0]);
        var increment = parts.Length > 1 ? ParseTime(parts[1]) : TimeSpan.Zero;
        
        return new Clock(baseTime, increment);
    }
    
    private static TimeSpan ParseTime(string time)
    {
        if (time.Contains(':'))
        {
            var timeParts = time.Split(':');
            if (timeParts.Length == 2)
            {
                var minutes = int.Parse(timeParts[0]);
                var seconds = int.Parse(timeParts[1]);
                return TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
            }
        }
        
        // Assume it's just minutes
        var minutesValue = int.Parse(time);
        return TimeSpan.FromMinutes(minutesValue);
    }
}
