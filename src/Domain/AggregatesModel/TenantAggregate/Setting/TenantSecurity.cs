namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantSecurity
    {
        public TenantPassword Password { get; set; } = new TenantPassword();
        public TenantSession Session { get; set; } = new TenantSession();
        public bool RequireMfaForAdmins { get; set; }
        public List<string> IpRestrictions { get; set; }
    }
}
