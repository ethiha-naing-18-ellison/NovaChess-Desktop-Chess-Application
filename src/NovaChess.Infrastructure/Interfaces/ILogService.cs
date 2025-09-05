namespace NovaChess.Infrastructure.Interfaces;

public interface ILogService
{
    void Debug(string message);
    void Information(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    void Fatal(string message, Exception? exception = null);
}
