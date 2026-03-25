using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate
{
    public class PaginatedList<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int PageIndex { get; }
        public int TotalPages { get; }
        public int TotalCount { get; }

        /// <summary>True when a previous page exists (PageIndex > 1).</summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>True when a next page exists.</summary>
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(IEnumerable<T> items, int count, int pageIndex, int pageSize)
        {
            TotalCount = count;
            PageIndex = pageIndex;
            TotalPages = pageSize > 0 ? (int)Math.Ceiling(count / (double)pageSize) : 0;
            Items = items.ToList();
        }

        /// <summary>
        /// Asynchronously create a paginated list from an <see cref="IQueryable{T}"/>.
        /// Executes a COUNT and a paged SELECT query against the database.
        /// </summary>
        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var count = await source.CountAsync(cancellationToken);
            var items = await source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
