using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;
using MonkoraEdge.Core.DotNet.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MonkoraEdge.Core.DotNet.Infrastructure
{
    /// <summary>
    /// Generic EF Core repository base class.
    /// Provides full sync + async CRUD plus audit/soft-delete handling.
    ///
    /// Usage:
    /// <code>
    /// public class OrderRepository : BaseRepository&lt;AppDbContext, Order&gt;, IOrderRepository
    /// {
    ///     public OrderRepository(AppDbContext ctx, ICurrentUserService currentUser)
    ///         : base(ctx, currentUser) { }
    /// }
    /// </code>
    /// </summary>
    public abstract class BaseRepository<TDbContext, TEntity> : IRepository<TEntity>
        where TDbContext : DbContext
        where TEntity : BaseEntity
    {
        protected readonly TDbContext Context;
        private readonly ICurrentUserService _currentUserService;

        // Preferred constructor inject ICurrentUserService
        protected BaseRepository(TDbContext dbContext, ICurrentUserService currentUserService)
        {
            Context = dbContext;
            _currentUserService = currentUserService;
        }

        // Fallback constructor for backwards compatibility / simple scenarios
        protected BaseRepository(TDbContext dbContext)
        {
            Context = dbContext;
        }

        // ================================================================
        // Sync reads
        // ================================================================

        public TEntity Get(Guid id) => Context.Set<TEntity>().Find(id);

        public IList<TEntity> List() => Context.Set<TEntity>().ToList();

        public IList<TEntity> List(Expression<Func<TEntity, bool>> expression)
            => Context.Set<TEntity>().Where(expression).ToList();

        // ================================================================
        // Async reads
        // ================================================================

        public async Task<TEntity> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => await Context.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);

        public async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
            => await Context.Set<TEntity>().FirstOrDefaultAsync(expression, cancellationToken);

        public async Task<IList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
            => await Context.Set<TEntity>().ToListAsync(cancellationToken);

        public async Task<IList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
            => await Context.Set<TEntity>().Where(expression).ToListAsync(cancellationToken);

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Context.Set<TEntity>().CountAsync(cancellationToken);

        public Task<int> CountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
            => Context.Set<TEntity>().CountAsync(expression, cancellationToken);

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
            => Context.Set<TEntity>().AnyAsync(e => e.Id == id, cancellationToken);

        public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
            => Context.Set<TEntity>().AnyAsync(expression, cancellationToken);

        // ================================================================
        // Specification-based reads
        // ================================================================

        public async Task<TEntity> GetAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default)
            => await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);

        public async Task<IList<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default)
            => await ApplySpecification(spec).ToListAsync(cancellationToken);

        public Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default)
            => ApplySpecification(spec).CountAsync(cancellationToken);

        // ================================================================
        // Sync writes
        // ================================================================

        public void Insert(TEntity entity)
        {
            SetAuditFields(entity);
            Context.Set<TEntity>().Add(entity);
        }

        public void InsertRange(List<TEntity> entities)
        {
            foreach (var entity in entities)
                SetAuditFields(entity);

            Context.Set<TEntity>().AddRange(entities);
        }

        public void Update(TEntity entity)
        {
            SetAuditFields(entity);
            Context.Entry(entity).State = EntityState.Modified;
        }

        public void UpdateRange(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                SetAuditFields(entity);
                Context.Entry(entity).State = EntityState.Modified;
            }
        }

        public void Delete(TEntity entity)
        {
            if (entity is ISoftDelete softDelete)
            {
                var now = DateTime.UtcNow;
                var userId = GetUserId();
                softDelete.DeletedAt = now;
                softDelete.DeletedBy = userId;
                entity.UpdatedAt = now;
                entity.UpdatedBy = userId;
            }
            else
            {
                Context.Set<TEntity>().Remove(entity);
            }
        }

        public void DeleteRange(List<TEntity> entities)
        {
            foreach (var entity in entities)
                Delete(entity);
        }

        // ================================================================
        // Async writes
        // ================================================================

        public async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            SetAuditFields(entity);
            await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
        }

        public async Task InsertRangeAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
                SetAuditFields(entity);

            await Context.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
        }

        // ================================================================
        // Helpers
        // ================================================================

        protected string GetUserId()
        {
            var userId = _currentUserService?.UserId;
            return string.IsNullOrEmpty(userId) ? "Anonymous" : userId;
        }

        /// <summary>
        /// Sets CreatedAt/By and UpdatedAt/By on the entity and any navigational BaseEntity children.
        /// </summary>
        public void SetAuditFields(BaseEntity entity)
        {
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            SetAuditFields(entity, visited, DateTime.UtcNow, GetUserId());
        }

        private void SetAuditFields(BaseEntity entity, HashSet<object> visited, DateTime now, string userId)
        {
            if (!visited.Add(entity)) return;

            // Walk navigation properties for child entities
            foreach (var prop in entity.GetType().GetProperties())
            {
                var value = prop.GetValue(entity);
                if (value is null) continue;

                if (prop.PropertyType.IsICollection() && value is IEnumerable<BaseEntity> children)
                {
                    foreach (var child in children.ToList())
                        SetAuditFields(child, visited, now, userId);
                }
                else if (value is BaseEntity child)
                {
                    SetAuditFields(child, visited, now, userId);
                }
            }

            if (entity.CreatedAt == default)
                entity.CreatedAt = now;

            if (string.IsNullOrEmpty(entity.CreatedBy))
                entity.CreatedBy = userId;

            entity.UpdatedAt = now;
            entity.UpdatedBy = userId;
        }

        // ================================================================
        // Specification helper
        // ================================================================

        /// <summary>
        /// Build an EF Core queryable from an <see cref="ISpecification{TEntity}"/>:
        /// applies criteria (Where), includes (Include / ThenInclude string paths), ordering, and paging.
        /// </summary>
        protected IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec)
        {
            var query = Context.Set<TEntity>().AsQueryable();

            if (spec.Criteria != null)
                query = query.Where(spec.Criteria);

            query = spec.Includes.Aggregate(query, (q, include) => q.Include(include));

            query = spec.IncludeStrings.Aggregate(query, (q, include) => q.Include(include));

            if (spec.OrderBy != null)
                query = query.OrderBy(spec.OrderBy);
            else if (spec.OrderByDescending != null)
                query = query.OrderByDescending(spec.OrderByDescending);

            if (spec.IsPagingEnabled)
                query = query.Skip(spec.Skip).Take(spec.Take);

            return query;
        }
    }
}
