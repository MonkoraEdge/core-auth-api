namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting
{
    public class TenantFeatures
    {
        public bool Mfa { get; set; }
        public TenantFileUpload FileUpload { get; set; } = new TenantFileUpload();  
        public TenantReports Reports { get; set; } = new TenantReports();
    }
}
