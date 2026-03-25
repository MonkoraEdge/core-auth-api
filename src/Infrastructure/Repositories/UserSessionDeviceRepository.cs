using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class UserSessionDeviceRepository : BaseRepository<AuthenticationDbContext, UserSessionDevice>, IUserSessionDeviceRepository
{
    public UserSessionDeviceRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<UserSessionDevice?> GetByIdAsync(Guid id) =>
        await Context.UserSessionDevices.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IEnumerable<UserSessionDevice>> GetByUserIdAsync(Guid userId) =>
        await Context.UserSessionDevices.Where(m => m.UserId == userId).ToListAsync();

    public async Task<UserSessionDevice?> GetByFingerprintAsync(Guid userId, string deviceFingerprint) =>
        await Context.UserSessionDevices.FirstOrDefaultAsync(m => m.UserId == userId && m.DeviceFingerprint == deviceFingerprint);
}
