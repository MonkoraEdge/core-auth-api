using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class UserTwoFactorSettingRepository : BaseRepository<AuthenticationDbContext, UserTwoFactorSetting>, IUserTwoFactorSettingRepository
{
    public UserTwoFactorSettingRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<UserTwoFactorSetting> GetByIdAsync(Guid id) =>
        await Context.UserTwoFactorSettings.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IEnumerable<UserTwoFactorSetting>> GetByUserIdAsync(Guid userId) =>
        await Context.UserTwoFactorSettings.Where(m => m.UserId == userId && m.IsActive).ToListAsync();

    public async Task<UserTwoFactorSetting> GetByUserIdAndDeviceTypeAsync(Guid userId, string deviceType) =>
        await Context.UserTwoFactorSettings.FirstOrDefaultAsync(m => m.UserId == userId && m.DeviceType == deviceType);
}
