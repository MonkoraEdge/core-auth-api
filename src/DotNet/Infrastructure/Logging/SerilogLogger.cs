using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using Serilog;

namespace MonkoraEdge.Core.DotNet.Infrastructure.Logging
{
    /// <summary>
    /// ILoggerService implementation backed by Serilog.
    /// Uses the globally configured static Serilog logger (Log.Logger).
    /// Configure Serilog in your application startup:
    /// <code>
    /// Log.Logger = new LoggerConfiguration()
    ///     .ReadFrom.Configuration(configuration)
    ///     .Enrich.FromLogContext()
    ///     .WriteTo.Console()
    ///     .CreateLogger();
    /// // Or via builder.Host.UseSerilog(...)
    /// </code>
    /// </summary>
    public class SerilogLogger<T> : ILoggerService<T>
    {
        private readonly ILogger _logger;

        public SerilogLogger()
        {
            // ForContext<T>() enriches every log entry with the source type name.
            // This relies on Log.Logger being configured externally (v. Program.cs).
            _logger = Log.ForContext<T>();
        }

        public void LogInformation(string message, params object[] args)
            => _logger.Information(message, args);

        public void LogWarning(string message, params object[] args)
            => _logger.Warning(message, args);

        public void LogError(string message, Exception ex, params object[] args)
            => _logger.Error(ex, message, args);

        public void LogDebug(string message, params object[] args)
            => _logger.Debug(message, args);

        public void LogCritical(string message, Exception ex, params object[] args)
            => _logger.Fatal(ex, message, args);
    }
}
