using System.Linq.Expressions;

namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    /// <summary>
    /// Encapsulates a query specification: criteria, eager-loading includes, ordering, and paging.
    /// Use with <see cref="IRepository{TEntity}"/> overloads that accept a specification.
    /// </summary>
    /// <typeparam name="T">The entity type the specification applies to.</typeparam>
    public interface ISpecification<T>
    {
        /// <summary>Where clause — null means no filtering.</summary>
        Expression<Func<T, bool>> Criteria { get; }

        /// <summary>Eager-load expressions passed to EF Core <c>.Include()</c>.</summary>
        List<Expression<Func<T, object>>> Includes { get; }

        /// <summary>String-based include paths (e.g. "Order.Items.Product").</summary>
        List<string> IncludeStrings { get; }

        /// <summary>Primary ascending sort — null means no ordering.</summary>
        Expression<Func<T, object>> OrderBy { get; }

        /// <summary>Primary descending sort — null means no ordering.</summary>
        Expression<Func<T, object>> OrderByDescending { get; }

        /// <summary>Number of records to skip. Only applied when <see cref="IsPagingEnabled"/> is true.</summary>
        int Skip { get; }

        /// <summary>Number of records to take. Only applied when <see cref="IsPagingEnabled"/> is true.</summary>
        int Take { get; }

        /// <summary>Whether paging (<see cref="Skip"/>/<see cref="Take"/>) is applied to the query.</summary>
        bool IsPagingEnabled { get; }
    }
}
