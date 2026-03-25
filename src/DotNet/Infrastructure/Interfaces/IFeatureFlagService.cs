namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    /// <summary>
    /// Feature flag service abstraction.
    /// Implementations can use Azure App Configuration, LaunchDarkly, config JSON,
    /// a database table, or any other feature management backend.
    ///
    /// Usage:
    /// <code>
    /// if (await _featureFlags.IsEnabledAsync("NewCheckout"))
    ///     return await HandleNewCheckout(cmd, ct);
    /// else
    ///     return await HandleLegacyCheckout(cmd, ct);
    /// </code>
    /// </summary>
    public interface IFeatureFlagService
    {
        /// <summary>Returns true if the named feature is enabled. Synchronous convenience overload.</summary>
        bool IsEnabled(string featureName);

        /// <summary>Returns true if the named feature is enabled (async for remote flag providers).</summary>
        Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);
    }
}
