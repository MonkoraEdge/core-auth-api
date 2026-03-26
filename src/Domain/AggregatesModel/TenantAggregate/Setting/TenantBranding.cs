using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantBranding
    {
        public Locale DisplayName { get; set; }
        public string LogoUrl { get; set; }
        public string PrimaryColor { get; set; }
        public string? FaviconUrl { get; set; }
    }
}
