using MonkoraEdge.Core.DotNet.Domain.SeedWork;
using System.Linq.Expressions;

namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    /// <summary>
    /// Generic repository abstraction for EF Core backed entities.
    /// Sync methods are provided for simple scenarios; prefer async equivalents.
    /// Use <see cref="ISpecification{TEntity}"/> overloads for complex queries with includes, ordering, or paging.
    /// </summary>
    public interface IRepository<TEntity> where TEntity : BaseEntity
    {
        // ---- Synchronous (simple reads) ----

        TEntity Get(Guid id);
        IList<TEntity> List();
        IList<TEntity> List(Expression<Func<TEntity, bool>> expression);

        // ---- Async (preferred) ----

        Task<TEntity> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task<TEntity> GetAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
        Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
        Task<IList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
        Task<IList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
        Task<IList<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
        Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        // ---- Write ----

        void Insert(TEntity entity);
        void InsertRange(List<TEntity> entities);
        Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task InsertRangeAsync(IList<TEntity> entities, CancellationToken cancellationToken = default);

        void Update(TEntity entity);
        void UpdateRange(List<TEntity> entities);

        void Delete(TEntity entity);
        void DeleteRange(List<TEntity> entities);
    }
}
