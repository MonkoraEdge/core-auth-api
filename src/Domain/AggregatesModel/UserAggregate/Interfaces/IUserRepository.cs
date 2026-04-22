using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;
using System.Linq.Expressions;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);

    /// <summary>DB-level paginated query — pushes Skip/Take to the database to avoid full table scans.</summary>
    Task<(IList<User> Items, int Total)> GetPagedAsync(
        Expression<Func<User, bool>> predicate, int page, int pageSize, bool ascending);
}
