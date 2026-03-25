using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate
{
    /// <summary>
    /// Standard pagination request model.
    /// Extend with a search/filter field in each feature's specific query.
    ///
    /// Usage:
    /// <code>
    /// public class GetOrdersQuery : PagedRequest, IRequest&lt;PaginatedList&lt;OrderDto&gt;&gt;
    /// {
    ///     public string? Status { get; set; }
    /// }
    /// </code>
    /// </summary>
    public class PagedRequest
    {
        private int _page = 1;
        private int _pageSize = AppConstants.DefaultPageSize;

        /// <summary>1-based page number. Defaults to 1.</summary>
        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        /// <summary>Items per page. Clamped to [1, MaxPageSize]. Defaults to DefaultPageSize.</summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1) _pageSize = AppConstants.DefaultPageSize;
                else if (value > AppConstants.MaxPageSize) _pageSize = AppConstants.MaxPageSize;
                else _pageSize = value;
            }
        }

        /// <summary>Property name to sort by (optional).</summary>
        public string SortBy { get; set; }

        /// <summary>Sort direction. True = descending.</summary>
        public bool SortDescending { get; set; }

        /// <summary>Free-text search keyword (optional).</summary>
        public string Search { get; set; }

        // ----------------------------------------------------------------
        // Computed helpers
        // ----------------------------------------------------------------

        /// <summary>Number of records to skip (for LINQ .Skip()).</summary>
        public int Skip => (Page - 1) * PageSize;

        /// <summary>Number of records to take (for LINQ .Take()).</summary>
        public int Take => PageSize;
    }
}
