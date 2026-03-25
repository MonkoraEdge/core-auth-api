namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantPlan
    {
        public string Tier { get; set; }
        public int UserLimit { get; set; }
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow;
    }
}
