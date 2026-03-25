using MonkoraEdge.Core.Auth.Domain.Repositories;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;
using MonkoraEdge.Core.DotNet.Infrastructure;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public abstract class AuthRepositoryBase<TEntity> : BaseRepository<AuthenticationDbContext, TEntity>, IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected AuthRepositoryBase(AuthenticationDbContext context) : base(context)
    {
    }
}