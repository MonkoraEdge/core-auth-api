using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantUpdateRequest
    {
        public Locale? TenantName { get; set; }
        public bool? IsActive { get; set; }
        public TenantSettings? Settings { get; set; }
    }
}
