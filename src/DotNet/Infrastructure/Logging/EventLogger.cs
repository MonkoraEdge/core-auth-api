using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace MonkoraEdge.Core.DotNet.Infrastructure.Logging
{
    /// <summary>
    /// Windows-only logger using Windows Event Log.
    /// On non-Windows platforms this class is a no-op to prevent PlatformNotSupportedException.
    /// Register only when running on Windows and with sufficient privileges.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class EventLogger<T> : ILoggerService<T>
    {
        private readonly string _source;

        public EventLogger()
        {
            _source = typeof(T).Name;
            if (!EventLog.SourceExists(_source))
            {
                EventLog.CreateEventSource(_source, "Application");
            }
        }

        public void LogInformation(string message, params object[] args)
            => EventLog.WriteEntry(_source, string.Format(message, args), EventLogEntryType.Information);

        public void LogWarning(string message, params object[] args)
            => EventLog.WriteEntry(_source, string.Format(message, args), EventLogEntryType.Warning);

        public void LogError(string message, Exception ex, params object[] args)
            => EventLog.WriteEntry(_source, $"{string.Format(message, args)}\n{ex}", EventLogEntryType.Error);

        public void LogDebug(string message, params object[] args)
            => EventLog.WriteEntry(_source, string.Format(message, args), EventLogEntryType.Information);

        public void LogCritical(string message, Exception ex, params object[] args)
            => EventLog.WriteEntry(_source, $"{string.Format(message, args)}\n{ex}", EventLogEntryType.FailureAudit);
    }
}
