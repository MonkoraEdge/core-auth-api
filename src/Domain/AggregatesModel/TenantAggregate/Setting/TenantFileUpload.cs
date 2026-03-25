namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantFileUpload
    {
        public bool Enabled { get; set; }
        public int MaxSizeMb { get; set; } = 1024 * 1024;
    }
}
