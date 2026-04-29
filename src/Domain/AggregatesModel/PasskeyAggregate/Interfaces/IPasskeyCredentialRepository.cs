using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.PasskeyAggregate.Interfaces;

public interface IPasskeyCredentialRepository : IRepository<PasskeyCredential>
{
    /// <summary>Returns a single credential by its primary key, or null if not found.</summary>
    Task<PasskeyCredential?> GetByIdAsync(Guid id);

    /// <summary>Returns all active passkeys belonging to a user.</summary>
    Task<IEnumerable<PasskeyCredential>> GetByUserIdAsync(Guid userId);

    /// <summary>Looks up a credential by its base64url-encoded ID.</summary>
    Task<PasskeyCredential?> GetByCredentialIdAsync(string credentialIdBase64Url);

    /// <summary>
    /// Atomically advances the signature counter and records the last-used timestamp.
    /// </summary>
    Task UpdateSignatureCounterAsync(Guid id, uint signatureCounter, DateTime lastUsedAt);
}
