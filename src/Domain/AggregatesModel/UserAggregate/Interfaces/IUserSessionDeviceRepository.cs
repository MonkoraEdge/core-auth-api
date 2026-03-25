using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IUserSessionDeviceRepository : IRepository<UserSessionDevice>
{
    Task<UserSessionDevice> GetByIdAsync(Guid id);
    Task<IEnumerable<UserSessionDevice>> GetByUserIdAsync(Guid userId);
    Task<UserSessionDevice> GetByFingerprintAsync(Guid userId, string fingerprint);
}
