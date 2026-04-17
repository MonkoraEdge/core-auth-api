using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate
{
    public class TenantResponse
    {
        public string Id { get; set; } = string.Empty;
        public string TenantCode { get; set; } = string.Empty;
        public Locale? TenantName { get; set; }
        public TenantSettings? Settings { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
