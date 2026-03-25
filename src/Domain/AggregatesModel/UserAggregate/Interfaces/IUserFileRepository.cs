using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IUserFileRepository : IRepository<UserFile>
{
    Task<UserFile> GetByIdAsync(Guid id);
    Task<IEnumerable<UserFile>> GetByUserIdAsync(Guid userId);
    Task<UserFile> GetActiveProfileByUserIdAsync(Guid userId);
}
