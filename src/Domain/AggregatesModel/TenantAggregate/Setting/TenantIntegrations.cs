namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantIntegrations
    {
        public TenantGoogleOauth GoogleOauth { get; set; } = new TenantGoogleOauth();
        public List<TenantWebhook> Webhooks { get; set; } = new List<TenantWebhook>();
    }
}
