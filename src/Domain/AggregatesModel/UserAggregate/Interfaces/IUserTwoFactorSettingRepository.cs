using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IUserTwoFactorSettingRepository : IRepository<UserTwoFactorSetting>
{
    Task<UserTwoFactorSetting> GetByIdAsync(Guid id);
    Task<IEnumerable<UserTwoFactorSetting>> GetByUserIdAsync(Guid userId);
    Task<UserTwoFactorSetting> GetByUserIdAndDeviceTypeAsync(Guid userId, string deviceType);
}
