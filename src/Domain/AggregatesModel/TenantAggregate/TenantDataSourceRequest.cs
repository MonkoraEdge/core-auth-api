using MonkoraEdge.Core.Auth.Domain.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.DataSourceAggregate;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate
{
    public class TenantDataSourceRequest : DataSourceRequest
    {
        public List<Sorting> Sorting { get; set; }
        public string Keyword { get; set; }
        public bool Status { get; set; }
    }
}
