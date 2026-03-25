namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantSession
    {
        public int TimeoutMinutes { get; set; }
        public bool PersistentSessions { get; set; }
    }
}
