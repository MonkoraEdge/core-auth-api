namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantCustom
    {
        public List<string> Tags { get; set; } = new List<string>();
        public TenantMetadata Metadata { get; set; } = new TenantMetadata();
    }
}
