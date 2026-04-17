
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate
{
    public class TenantCreateRequest
    {
        /// <summary>Unique tenant code (slug). e.g. "ACME"</summary>
        public string TenantCode { get; set; } = string.Empty;
        /// <summary>Multi-language display name.</summary>
        public Locale TenantName { get; set; } = new();
        /// <summary>Initial active state.</summary>
        public bool IsActive { get; set; } = true;
        /// <summary>Optional tenant settings. Defaults applied when null.</summary>
        public TenantSettings? Settings { get; set; }
    }
}
