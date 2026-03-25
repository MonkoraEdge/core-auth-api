using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces.Jobs;

namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    /// <summary>
    /// Platform-agnostic abstraction for background job scheduling.
    /// Implementations can wrap Hangfire, Quartz.NET, .NET BackgroundService, etc.
    ///
    /// Usage:
    /// <code>
    /// // Fire and forget
    /// _jobs.Enqueue&lt;SendEmailJob&gt;();
    ///
    /// // Delayed
    /// _jobs.Schedule&lt;SyncDataJob&gt;(TimeSpan.FromMinutes(30));
    ///
    /// // Recurring
    /// _jobs.AddOrUpdateRecurring&lt;CleanupJob&gt;("cleanup-daily", "0 2 * * *");
    /// </code>
    /// </summary>
    public interface IBackgroundJobService
    {
        /// <summary>Enqueue a job to run as soon as possible (fire-and-forget).</summary>
        string Enqueue<TJob>() where TJob : IJob;

        /// <summary>Schedule a job to run after the specified delay.</summary>
        string Schedule<TJob>(TimeSpan delay) where TJob : IJob;

        /// <summary>Add or update a recurring job identified by <paramref name="jobId"/>.</summary>
        void AddOrUpdateRecurring<TJob>(string jobId, string cronExpression) where TJob : IScheduledJob;

        /// <summary>Remove a scheduled or recurring job.</summary>
        void Delete(string jobId);

        /// <summary>Trigger a recurring job immediately (run now outside schedule).</summary>
        void TriggerNow(string jobId);
    }
}
