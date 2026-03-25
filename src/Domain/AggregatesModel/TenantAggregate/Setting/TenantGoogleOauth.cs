namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantGoogleOauth
    {
        public bool Enabled { get; set; }
        public string ClientId { get; set; }
        public List<string> RedirectUris { get; set; } = new List<string>();
    }
}
