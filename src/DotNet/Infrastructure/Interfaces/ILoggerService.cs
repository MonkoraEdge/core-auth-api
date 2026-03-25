namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    public interface ILoggerService<T>
    {
        void LogInformation(string message, params object[] args);

        void LogWarning(string message, params object[] args);

        void LogError(string message, Exception ex, params object[] args);

        void LogDebug(string message, params object[] args);

        void LogCritical(string message, Exception ex, params object[] args);
    }
}
