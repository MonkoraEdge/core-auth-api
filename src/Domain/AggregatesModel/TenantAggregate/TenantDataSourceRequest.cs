using MonkoraEdge.Core.DotNet.AggregatesModel.DataSourceAggregate;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate
{
    public class TenantDataSourceRequest : DataSourceRequest
    {
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
