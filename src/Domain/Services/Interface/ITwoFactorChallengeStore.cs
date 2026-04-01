namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// Short-lived store for pending 2FA login challenges.
/// Maps an opaque token (returned to the client after a password-correct + 2FA-required response)
/// to the authenticated user's ID. Consuming a token removes it — tokens are single-use.
/// </summary>
public interface ITwoFactorChallengeStore
{
    /// <summary>Persist a token → userId binding for <paramref name="expiry"/>.</summary>
    void Store(string token, Guid userId, TimeSpan expiry);

    /// <summary>
    /// Look up the userId for <paramref name="token"/> and atomically remove it.
    /// Returns <c>null</c> if the token does not exist or has expired.
    /// </summary>
    Guid? Consume(string token);
}
