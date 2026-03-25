namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantLimits
    {
        public int ApiCallsPerMinute { get; set; }
        public int StorageMb { get; set; } 
    }
}
