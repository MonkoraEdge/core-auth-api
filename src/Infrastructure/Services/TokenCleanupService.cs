using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services;

/// <summary>
/// Background service that periodically purges expired tokens from the database.
/// Prevents unbounded table growth that degrades token lookup performance.
/// </summary>
public sealed class TokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    // Retain expired tokens for 7 days for audit/forensic purposes before hard delete
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenCleanupService> _logger;

    public TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay startup so the app has time to fully initialize
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeExpiredTokensAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown — not an error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token cleanup cycle failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task PurgeExpiredTokensAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();

        var cutoff = DateTime.UtcNow.Subtract(RetentionPeriod);

        // Delete expired/revoked access tokens older than retention window
        var accessDeleted = await db.AccessTokens
            .Where(t => t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(ct);

        // Delete expired/revoked refresh tokens older than retention window
        var refreshDeleted = await db.RefreshTokens
            .Where(t => t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(ct);

        // Delete expired authorization codes (these are short-lived: 60s)
        var codeDeleted = await db.AuthorizationCodes
            .Where(t => t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(ct);

        // Revoked tokens table — purge entries where expiry is set and has passed
        var revokedDeleted = await db.RevokedTokens
            .Where(t => t.ExpiresAt.HasValue && t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(ct);

        // Revoked tokens with no ExpiresAt (e.g. from family revocation cascade) — purge
        // based on RevokedAt so they don't accumulate indefinitely in the table.
        var revokedNullExpiryDeleted = await db.RevokedTokens
            .Where(t => !t.ExpiresAt.HasValue && t.RevokedAt < cutoff)
            .ExecuteDeleteAsync(ct);

        if (accessDeleted + refreshDeleted + codeDeleted + revokedDeleted + revokedNullExpiryDeleted > 0)
        {
            _logger.LogInformation(
                "Token cleanup: removed {Access} access tokens, {Refresh} refresh tokens, {Codes} auth codes, {Revoked} revoked tokens ({RevokedNullExpiry} with no expiry).",
                accessDeleted, refreshDeleted, codeDeleted, revokedDeleted, revokedNullExpiryDeleted);
        }
    }
}
