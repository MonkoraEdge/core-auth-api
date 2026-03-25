using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using System.Linq.Expressions;

namespace MonkoraEdge.Core.DotNet.Domain.SeedWork
{
    /// <summary>
    /// Base class for building reusable, composable query specifications.
    /// Inherit and call the protected helpers to build the specification.
    ///
    /// Example:
    /// <code>
    /// public class ActiveOrdersSpec : BaseSpecification&lt;Order&gt;
    /// {
    ///     public ActiveOrdersSpec(Guid customerId)
    ///         : base(o => o.CustomerId == customerId &amp;&amp; o.Status == OrderStatus.Active)
    ///     {
    ///         AddInclude(o => o.Items);
    ///         ApplyOrderByDescending(o => o.CreatedAt);
    ///     }
    /// }
    ///
    /// // Usage
    /// var orders = await _orderRepo.ListAsync(new ActiveOrdersSpec(customerId));
    /// </code>
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        protected BaseSpecification() { }

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        public Expression<Func<T, bool>> Criteria { get; private set; }
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        public List<string> IncludeStrings { get; } = new();
        public Expression<Func<T, object>> OrderBy { get; private set; }
        public Expression<Func<T, object>> OrderByDescending { get; private set; }
        public int Skip { get; private set; }
        public int Take { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        // ----------------------------------------------------------------
        // Protected configuration helpers
        // ----------------------------------------------------------------

        protected void AddCriteria(Expression<Func<T, bool>> criteria)
            => Criteria = criteria;

        protected void AddInclude(Expression<Func<T, object>> include)
            => Includes.Add(include);

        protected void AddInclude(string includeString)
            => IncludeStrings.Add(includeString);

        protected void ApplyOrderBy(Expression<Func<T, object>> orderBy)
            => OrderBy = orderBy;

        protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescending)
            => OrderByDescending = orderByDescending;

        /// <summary>
        /// Apply paging. <paramref name="pageNumber"/> is 1-based.
        /// </summary>
        protected void ApplyPaging(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            Skip = (pageNumber - 1) * pageSize;
            Take = pageSize;
            IsPagingEnabled = true;
        }

        /// <summary>Apply paging using raw skip/take values.</summary>
        protected void ApplyRawPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        // ----------------------------------------------------------------
        // Specification composition (And / Or)
        // ----------------------------------------------------------------

        /// <summary>
        /// Combine this specification with <paramref name="other"/> using logical AND.
        /// Returns a new anonymous specification that satisfies both criteria.
        /// Includes, ordering, and paging from the left-hand spec are preserved;
        /// right-hand includes are merged.
        /// </summary>
        public BaseSpecification<T> And(BaseSpecification<T> other)
        {
            var combined = new CompositeSpecification<T>(
                CombineExpressions(Criteria, other.Criteria, ExpressionType.AndAlso));

            MergeIncludes(combined, this);
            MergeIncludes(combined, other);
            return combined;
        }

        /// <summary>
        /// Combine this specification with <paramref name="other"/> using logical OR.
        /// </summary>
        public BaseSpecification<T> Or(BaseSpecification<T> other)
        {
            var combined = new CompositeSpecification<T>(
                CombineExpressions(Criteria, other.Criteria, ExpressionType.OrElse));

            MergeIncludes(combined, this);
            return combined;
        }

        private static Expression<Func<T, bool>> CombineExpressions(
            Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right,
            ExpressionType type)
        {
            if (left == null) return right;
            if (right == null) return left;

            var param = Expression.Parameter(typeof(T));
            var leftBody = new ParameterReplacer(left.Parameters[0], param).Visit(left.Body);
            var rightBody = new ParameterReplacer(right.Parameters[0], param).Visit(right.Body);
            var body = type == ExpressionType.AndAlso
                ? Expression.AndAlso(leftBody, rightBody)
                : Expression.OrElse(leftBody, rightBody);
            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        private static void MergeIncludes(BaseSpecification<T> target, BaseSpecification<T> source)
        {
            foreach (var include in source.Includes)
                target.AddInclude(include);
            foreach (var include in source.IncludeStrings)
                target.AddInclude(include);
        }

        // Internal helper to rewrite lambda parameter references
        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _old, _new;
            public ParameterReplacer(ParameterExpression old, ParameterExpression @new)
                => (_old, _new) = (old, @new);
            protected override Expression VisitParameter(ParameterExpression node)
                => node == _old ? _new : base.VisitParameter(node);
        }

        // Internal anonymous specification used for composition results
        private sealed class CompositeSpecification<TEntity> : BaseSpecification<TEntity>
        {
            public CompositeSpecification(Expression<Func<TEntity, bool>> criteria)
                : base(criteria) { }
        }
    }
}
