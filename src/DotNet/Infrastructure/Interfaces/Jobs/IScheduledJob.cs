namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces.Jobs
{
    /// <summary>
    /// Marker interface for a recurring scheduled job.
    /// Extend <see cref="IJob"/> with a cron expression definition.
    /// The cron expression is used by the background job infrastructure (Hangfire, Quartz, etc.)
    /// to schedule recurring execution.
    ///
    /// Example cron expressions:
    /// <list type="bullet">
    ///   <item><c>"0 * * * *"</c> — every hour at :00</item>
    ///   <item><c>"0 2 * * *"</c> — every day at 02:00</item>
    ///   <item><c>"0 0 * * 0"</c> — every Sunday at midnight</item>
    /// </list>
    /// </summary>
    public interface IScheduledJob : IJob
    {
        /// <summary>Cron expression defining when this job runs.</summary>
        string CronExpression { get; }
    }
}
