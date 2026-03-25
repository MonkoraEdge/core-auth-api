namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantPassword
    {
        public int MinLength { get; set; }
        public bool RequireUpper { get; set; }
        public bool RequireNumbers { get; set; }
        public bool RequireSpecial { get; set; }
    }
}
