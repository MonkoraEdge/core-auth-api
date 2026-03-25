namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantEmail
    {
        public string SenderName { get; set; }
        public string SenderAddress { get; set; }
        public string ReplyTo { get; set; }
        public TenantTemplates Templates { get; set; } = new TenantTemplates();
    }
}
