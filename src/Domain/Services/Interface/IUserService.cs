using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(Guid id);
    Task<(List<UserResponse> Items, int Total)> GetListAsync(UserDataSourceRequest request);
    Task<CreateResponse> CreateAsync(UserCreateRequest request, string? createdBy);
    Task<UpdateResponse> UpdateAsync(Guid id, UserUpdateRequest request, string? updatedBy);
    Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy);
    Task<UpdateResponse> ActivateAsync(Guid id, string? updatedBy);
    Task<UpdateResponse> DeactivateAsync(Guid id, string? updatedBy);
    Task<UpdateResponse> AssignRolesAsync(Guid userId, AssignRoleRequest request, string? updatedBy);
    Task<UpdateResponse> RemoveRolesAsync(Guid userId, List<Guid> roleIds, string? updatedBy);
    Task<List<UserResponse>> GetByTenantIdAsync(Guid tenantId);

    // Session management
    Task<List<UserSessionResponse>> GetSessionsAsync(Guid userId);
    Task<DeleteResponse> RevokeSessionAsync(Guid userId, Guid sessionId);
    Task<DeleteResponse> RevokeAllSessionsAsync(Guid userId);

    // Device management
    Task<List<UserDeviceResponse>> GetDevicesAsync(Guid userId);
    Task<UpdateResponse> TrustDeviceAsync(Guid userId, Guid deviceId);
    Task<UpdateResponse> BlockDeviceAsync(Guid userId, Guid deviceId);
    Task<DeleteResponse> RevokeDeviceAsync(Guid userId, Guid deviceId);
}
