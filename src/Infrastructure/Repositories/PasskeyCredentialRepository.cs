using Microsoft.EntityFrameworkCore;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.PasskeyAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class PasskeyCredentialRepository
    : AuthRepositoryBase<PasskeyCredential>, IPasskeyCredentialRepository
{
    public PasskeyCredentialRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<PasskeyCredential?> GetByIdAsync(Guid id) =>
        await Context.PasskeyCredentials.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<PasskeyCredential>> GetByUserIdAsync(Guid userId) =>
        await Context.PasskeyCredentials
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

    public async Task<PasskeyCredential?> GetByCredentialIdAsync(string credentialIdBase64Url) =>
        await Context.PasskeyCredentials
            .FirstOrDefaultAsync(c => c.CredentialIdBase64Url == credentialIdBase64Url);

    public async Task UpdateSignatureCounterAsync(Guid id, uint signatureCounter, DateTime lastUsedAt)
    {
        await Context.PasskeyCredentials
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.SignatureCounter, signatureCounter)
                .SetProperty(c => c.LastUsedAt, lastUsedAt));
    }
}
