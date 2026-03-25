using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ClientAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface IClientService
{
    Task<ClientResponse> GetByIdAsync(Guid id);
    Task<ClientResponse> GetByClientIdAsync(string clientId);
    Task<(List<ClientResponse> Items, int Total)> GetListAsync(Guid? tenantId, int page, int pageSize, string? search);
    Task<(ClientResponse Client, ClientSecretResponse Secret)> CreateAsync(ClientCreateRequest request, string? createdBy);
    Task<UpdateResponse> UpdateAsync(Guid id, ClientUpdateRequest request, string? updatedBy);
    Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy);
    Task<ClientSecretResponse> RotateSecretAsync(Guid id, string? updatedBy);
    Task<UpdateResponse> ActivateAsync(Guid id);
    Task<UpdateResponse> DeactivateAsync(Guid id);
}
