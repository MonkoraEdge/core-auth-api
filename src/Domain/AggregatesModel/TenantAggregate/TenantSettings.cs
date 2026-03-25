using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate
{
    public class TenantSettings
    {
        public TenantBranding Branding { get; set; }
        public TenantFeatures Features { get; set; }
        public TenantPlan Plan { get; set; }
        public TenantLocale Locale { get; set; }
        public TenantSecurity Security { get; set; }
        public TenantEmail Email { get; set; }
        public TenantIntegrations Integrations { get; set; }
        public TenantPolicies Policies { get; set; }
        public TenantLimits Limits { get; set; }
        public TenantCustom Custom { get; set; }
    }
}
