using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ScopeAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface IScopeService
{
    Task<List<ScopeResponse>> GetAllAsync(bool? activeOnly = true);
    Task<ScopeResponse> GetByIdAsync(Guid id);
    Task<CreateResponse> CreateAsync(ScopeCreateRequest request, string? createdBy);
    Task<UpdateResponse> UpdateAsync(Guid id, ScopeUpdateRequest request, string? updatedBy);
    Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy);
}

public interface IRoleService
{
    Task<List<RoleResponse>> GetListAsync(Guid? tenantId, bool? activeOnly = true);
    Task<RoleResponse> GetByIdAsync(Guid id);
    Task<CreateResponse> CreateAsync(RoleCreateRequest request, string? createdBy);
    Task<UpdateResponse> UpdateAsync(Guid id, RoleUpdateRequest request, string? updatedBy);
    Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy);
    Task<UpdateResponse> AssignPermissionsAsync(Guid roleId, AssignPermissionRequest request, string? updatedBy);
    Task<UpdateResponse> RemovePermissionsAsync(Guid roleId, List<Guid> permissionIds, string? updatedBy);
}

public interface IPermissionService
{
    Task<List<PermissionResponse>> GetListAsync(Guid? tenantId, bool? activeOnly = true);
    Task<PermissionResponse> GetByIdAsync(Guid id);
    Task<CreateResponse> CreateAsync(PermissionCreateRequest request, string? createdBy);
    Task<UpdateResponse> UpdateAsync(Guid id, PermissionUpdateRequest request, string? updatedBy);
    Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy);
}

public interface IApiKeyService
{
    Task<List<ApiKeyResponse>> GetByUserIdAsync(Guid userId);
    Task<List<ApiKeyResponse>> GetByClientIdAsync(Guid clientId);
    Task<ApiKeyResponse> GetByIdAsync(Guid id);
    Task<ApiKeyCreatedResponse> CreateAsync(ApiKeyCreateRequest request, string? createdBy);
    Task<DeleteResponse> RevokeAsync(Guid id, string? revokedBy);
    Task<UpdateResponse> UpdateLastUsedAsync(Guid id);
    /// <summary>Validate API key and return response if valid</summary>
    Task<ApiKeyResponse?> ValidateAsync(string rawKey);
}

public interface IProviderService
{
    Task<List<ProviderResponse>> GetAllAsync(bool? activeOnly = true);
    Task<ProviderResponse> GetByIdAsync(Guid id);
    Task<CreateResponse> CreateAsync(ProviderCreateRequest request, string? createdBy);
    Task<UpdateResponse> UpdateAsync(Guid id, ProviderUpdateRequest request, string? updatedBy);
    Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy);
}

public interface IAgreementService
{
    Task<List<AgreementResponse>> GetListAsync(Guid? tenantId, bool? activeOnly = true);
    Task<AgreementResponse> GetByIdAsync(Guid id);
    Task<CreateResponse> CreateAsync(AgreementCreateRequest request, string? createdBy);
    Task<UpdateResponse> UpdateAsync(Guid id, AgreementUpdateRequest request, string? updatedBy);
    Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy);
}
