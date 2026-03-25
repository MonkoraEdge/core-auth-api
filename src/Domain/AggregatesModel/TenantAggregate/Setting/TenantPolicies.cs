namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantPolicies
    {
        public int DataRetentionDays { get; set; }
        public bool ConsentRequired { get; set; }
        public List<string> AllowedEmailDomains { get; set; } = new List<string>();
    }
}
