using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.DataSourceAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface ITenantService
{
    Task<CreateResponse> CreateTenantAsync(TenantCreateRequest request);
    Task<DataSourceResponse<TenantDataSourceResponse>> GetTenantsAsync(TenantDataSourceRequest request);
    Task<TenantResponse> GetTenantByIdAsync(Guid id);
    Task<UpdateResponse> UpdateTenantAsync(Guid id, TenantUpdateRequest request);
    Task<DeleteResponse> DeleteTenantAsync(Guid id);
}
