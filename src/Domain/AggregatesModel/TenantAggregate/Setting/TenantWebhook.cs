namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantWebhook
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public bool Active { get; set; }
    }
}
