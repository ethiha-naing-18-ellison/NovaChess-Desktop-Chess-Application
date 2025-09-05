using Serilog;
using Serilog.Events;
using NovaChess.Infrastructure.Interfaces;

namespace NovaChess.Infrastructure.Services;

public class LogService : ILogService, IDisposable
{
    private readonly ILogger _logger;
    
    public LogService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var novaChessPath = Path.Combine(appDataPath, "NovaChess");
        var logsPath = Path.Combine(novaChessPath, "logs");
        
        if (!Directory.Exists(logsPath))
            Directory.CreateDirectory(logsPath);
            
        var logFilePath = Path.Combine(logsPath, "novachess-.log");
        
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
            
        Information("LogService initialized");
    }
    
    public void Debug(string message)
    {
        _logger.Debug(message);
    }
    
    public void Information(string message)
    {
        _logger.Information(message);
    }
    
    public void Warning(string message)
    {
        _logger.Warning(message);
    }
    
    public void Error(string message, Exception? exception = null)
    {
        if (exception != null)
            _logger.Error(exception, message);
        else
            _logger.Error(message);
    }
    
    public void Fatal(string message, Exception? exception = null)
    {
        if (exception != null)
            _logger.Fatal(exception, message);
        else
            _logger.Fatal(message);
    }
    
    public void Dispose()
    {
        // Serilog logger doesn't need explicit disposal
    }
}
