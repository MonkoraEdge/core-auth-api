using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ScopeAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using System.Security.Cryptography;

namespace MonkoraEdge.Core.Auth.Domain.Services;

// ─── ScopeService ─────────────────────────────────────────────────────────────

public class ScopeService : IScopeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScopeRepository _scopeRepo;

    public ScopeService(IUnitOfWork unitOfWork, IScopeRepository scopeRepo)
    {
        _unitOfWork = unitOfWork;
        _scopeRepo = scopeRepo;
    }

    public async Task<List<ScopeResponse>> GetAllAsync(bool? activeOnly = true)
    {
        var scopes = activeOnly == true
            ? await _scopeRepo.GetAllActiveAsync()
            : await _scopeRepo.ListAsync();
        return scopes.Select(s => MapToResponse(s)).ToList();
    }

    public async Task<ScopeResponse> GetByIdAsync(Guid id)
    {
        var scope = await _scopeRepo.GetByIdAsync(id);
        if (scope == null) throw new CustomHttpBadRequestException("scope", "Scope not found.");
        return MapToResponse(scope);
    }

    public async Task<CreateResponse> CreateAsync(ScopeCreateRequest request, string? createdBy)
    {
        var existing = await _scopeRepo.GetByScopeNameAsync(request.ScopeName);
        if (existing != null) throw new CustomHttpBadRequestException("scope", $"Scope '{request.ScopeName}' already exists.");

        var scope = new Scope
        {
            ScopeName = request.ScopeName,
            ScopeType = request.ScopeType,
            Claims = request.Claims,
            IsSystemScope = request.IsSystemScope,
            IsActive = true
        };
        _scopeRepo.Insert(scope);
        await _unitOfWork.SaveChangesAsync();
        return new CreateResponse { Id = scope.Id, IsSuccess = true, Message = "Scope created." };
    }

    public async Task<UpdateResponse> UpdateAsync(Guid id, ScopeUpdateRequest request, string? updatedBy)
    {
        var scope = await _scopeRepo.GetByIdAsync(id);
        if (scope == null) throw new CustomHttpBadRequestException("scope", "Scope not found.");
        if (scope.IsSystemScope) throw new CustomHttpBadRequestException("scope", "System scopes cannot be modified.");
        if (request.Claims != null) scope.Claims = request.Claims;
        if (request.IsActive.HasValue) scope.IsActive = request.IsActive.Value;
        _scopeRepo.Update(scope);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = scope.Id, IsSuccess = true, Message = "Scope updated." };
    }

    public async Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy)
    {
        var scope = await _scopeRepo.GetByIdAsync(id);
        if (scope == null) throw new CustomHttpBadRequestException("scope", "Scope not found.");
        if (scope.IsSystemScope) throw new CustomHttpBadRequestException("scope", "System scopes cannot be deleted.");
        scope.DeletedAt = DateTime.UtcNow;
        scope.DeletedBy = deletedBy;
        scope.IsActive = false;
        _scopeRepo.Update(scope);
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = scope.Id, IsSuccess = true, Message = "Scope deleted." };
    }

    private static ScopeResponse MapToResponse(Scope s) => new()
    {
        Id = s.Id.ToString(),
        ScopeName = s.ScopeName,
        ScopeType = s.ScopeType,
        Claims = s.Claims,
        IsSystemScope = s.IsSystemScope,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt
    };
}

// ─── RoleService ──────────────────────────────────────────────────────────────

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRoleRepository _roleRepo;
    private readonly IPermissionRepository _permissionRepo;
    private readonly IRolePermissionRepository _rolePermissionRepo;

    public RoleService(
        IUnitOfWork unitOfWork,
        IRoleRepository roleRepo,
        IPermissionRepository permissionRepo,
        IRolePermissionRepository rolePermissionRepo)
    {
        _unitOfWork = unitOfWork;
        _roleRepo = roleRepo;
        _permissionRepo = permissionRepo;
        _rolePermissionRepo = rolePermissionRepo;
    }

    public async Task<List<RoleResponse>> GetListAsync(Guid? tenantId, bool? activeOnly = true)
    {
        var roles = await _roleRepo.GetByTenantIdAsync(tenantId);
        if (activeOnly == true) roles = roles.Where(r => r.IsActive).ToList();
        var result = new List<RoleResponse>();
        foreach (var r in roles)
            result.Add(await MapToResponseAsync(r));
        return result;
    }

    public async Task<RoleResponse> GetByIdAsync(Guid id)
    {
        var role = await _roleRepo.GetByIdAsync(id);
        if (role == null) throw new CustomHttpBadRequestException("role", "Role not found.");
        return await MapToResponseAsync(role);
    }

    public async Task<CreateResponse> CreateAsync(RoleCreateRequest request, string? createdBy)
    {
        var existing = await _roleRepo.GetByRoleCodeAsync(request.RoleCode, request.TenantId);
        if (existing != null) throw new CustomHttpBadRequestException("role", $"Role code '{request.RoleCode}' already exists.");

        var role = new Role
        {
            TenantId = request.TenantId,
            RoleCode = request.RoleCode.ToUpperInvariant(),
            IsActive = true
        };
        _roleRepo.Insert(role);
        await _unitOfWork.SaveChangesAsync();
        return new CreateResponse { Id = role.Id, IsSuccess = true, Message = "Role created." };
    }

    public async Task<UpdateResponse> UpdateAsync(Guid id, RoleUpdateRequest request, string? updatedBy)
    {
        var role = await _roleRepo.GetByIdAsync(id);
        if (role == null) throw new CustomHttpBadRequestException("role", "Role not found.");
        if (request.IsActive.HasValue) role.IsActive = request.IsActive.Value;
        _roleRepo.Update(role);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = role.Id, IsSuccess = true, Message = "Role updated." };
    }

    public async Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy)
    {
        var role = await _roleRepo.GetByIdAsync(id);
        if (role == null) throw new CustomHttpBadRequestException("role", "Role not found.");
        role.DeletedAt = DateTime.UtcNow;
        role.DeletedBy = deletedBy;
        role.IsActive = false;
        _roleRepo.Update(role);
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = role.Id, IsSuccess = true, Message = "Role deleted." };
    }

    public async Task<UpdateResponse> AssignPermissionsAsync(Guid roleId, AssignPermissionRequest request, string? updatedBy)
    {
        var role = await _roleRepo.GetByIdAsync(roleId);
        if (role == null) throw new CustomHttpBadRequestException("role", "Role not found.");

        var existing = await _rolePermissionRepo.GetByRoleIdAsync(roleId);

        foreach (var permId in request.PermissionIds)
        {
            if (existing.Any(rp => rp.PermissionId == permId)) continue;
            var perm = await _permissionRepo.GetByIdAsync(permId);
            if (perm == null) throw new CustomHttpBadRequestException("role", $"Permission {permId} not found.");
            _rolePermissionRepo.Insert(new RolePermission { RoleId = roleId, PermissionId = permId });
        }

        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = roleId, IsSuccess = true, Message = "Permissions assigned." };
    }

    public async Task<UpdateResponse> RemovePermissionsAsync(Guid roleId, List<Guid> permissionIds, string? updatedBy)
    {
        var existing = await _rolePermissionRepo.GetByRoleIdAsync(roleId);
        var toRemove = existing.Where(rp => permissionIds.Contains(rp.PermissionId)).ToList();
        if (toRemove.Any()) _rolePermissionRepo.DeleteRange(toRemove.ToList());
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = roleId, IsSuccess = true, Message = "Permissions removed." };
    }

    private async Task<RoleResponse> MapToResponseAsync(Role role)
    {
        var rolePerms = await _rolePermissionRepo.GetByRoleIdAsync(role.Id);
        var permissions = new List<PermissionResponse>();
        foreach (var rp in rolePerms)
        {
            var perm = await _permissionRepo.GetByIdAsync(rp.PermissionId);
            if (perm != null)
                permissions.Add(new PermissionResponse
                {
                    Id = perm.Id.ToString(),
                    PermissionCode = perm.PermissionCode,
                    PermissionName = perm.PermissionCode,
                    Resource = perm.Resource ?? string.Empty,
                    Action = perm.Action ?? string.Empty,
                    IsActive = perm.IsActive
                });
        }

        return new RoleResponse
        {
            Id = role.Id.ToString(),
            TenantId = role.TenantId?.ToString(),
            RoleCode = role.RoleCode,
            RoleName = role.RoleCode,
            IsActive = role.IsActive,
            Permissions = permissions,
            CreatedAt = role.CreatedAt
        };
    }
}

// ─── PermissionService ────────────────────────────────────────────────────────

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionRepository _permissionRepo;

    public PermissionService(IUnitOfWork unitOfWork, IPermissionRepository permissionRepo)
    {
        _unitOfWork = unitOfWork;
        _permissionRepo = permissionRepo;
    }

    public async Task<List<PermissionResponse>> GetListAsync(Guid? tenantId, bool? activeOnly = true)
    {
        var perms = await _permissionRepo.GetByTenantIdAsync(tenantId);
        if (activeOnly == true) perms = perms.Where(p => p.IsActive).ToList();
        return perms.Select(MapToResponse).ToList();
    }

    public async Task<PermissionResponse> GetByIdAsync(Guid id)
    {
        var perm = await _permissionRepo.GetByIdAsync(id);
        if (perm == null) throw new CustomHttpBadRequestException("permission", "Permission not found.");
        return MapToResponse(perm);
    }

    public async Task<CreateResponse> CreateAsync(PermissionCreateRequest request, string? createdBy)
    {
        var existing = await _permissionRepo.GetByPermissionCodeAsync(request.PermissionCode, request.TenantId);
        if (existing != null) throw new CustomHttpBadRequestException("permission", $"Permission code '{request.PermissionCode}' already exists.");

        var perm = new Permission
        {
            TenantId = request.TenantId,
            PermissionCode = request.PermissionCode.ToUpperInvariant(),
            Resource = request.Resource,
            Action = request.Action,
            IsActive = true
        };
        _permissionRepo.Insert(perm);
        await _unitOfWork.SaveChangesAsync();
        return new CreateResponse { Id = perm.Id, IsSuccess = true, Message = "Permission created." };
    }

    public async Task<UpdateResponse> UpdateAsync(Guid id, PermissionUpdateRequest request, string? updatedBy)
    {
        var perm = await _permissionRepo.GetByIdAsync(id);
        if (perm == null) throw new CustomHttpBadRequestException("permission", "Permission not found.");
        if (request.Resource != null) perm.Resource = request.Resource;
        if (request.Action != null) perm.Action = request.Action;
        if (request.IsActive.HasValue) perm.IsActive = request.IsActive.Value;
        _permissionRepo.Update(perm);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = perm.Id, IsSuccess = true, Message = "Permission updated." };
    }

    public async Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy)
    {
        var perm = await _permissionRepo.GetByIdAsync(id);
        if (perm == null) throw new CustomHttpBadRequestException("permission", "Permission not found.");
        perm.DeletedAt = DateTime.UtcNow;
        perm.DeletedBy = deletedBy;
        perm.IsActive = false;
        _permissionRepo.Update(perm);
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = perm.Id, IsSuccess = true, Message = "Permission deleted." };
    }

    private static PermissionResponse MapToResponse(Permission p) => new()
    {
        Id = p.Id.ToString(),
        PermissionCode = p.PermissionCode,
        PermissionName = p.PermissionCode,
        Resource = p.Resource ?? string.Empty,
        Action = p.Action ?? string.Empty,
        IsActive = p.IsActive
    };
}

// ─── ApiKeyService ────────────────────────────────────────────────────────────

public class ApiKeyService : IApiKeyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApiKeyRepository _apiKeyRepo;
    private readonly IPasswordService _passwordService;

    public ApiKeyService(IUnitOfWork unitOfWork, IApiKeyRepository apiKeyRepo, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _apiKeyRepo = apiKeyRepo;
        _passwordService = passwordService;
    }

    public async Task<List<ApiKeyResponse>> GetByUserIdAsync(Guid userId)
    {
        var keys = await _apiKeyRepo.GetByUserIdAsync(userId);
        return keys.Where(k => k.DeletedAt == null).Select(MapToResponse).ToList();
    }

    public async Task<List<ApiKeyResponse>> GetByClientIdAsync(Guid clientId)
    {
        var keys = await _apiKeyRepo.GetByClientIdAsync(clientId);
        return keys.Where(k => k.DeletedAt == null).Select(MapToResponse).ToList();
    }

    public async Task<ApiKeyResponse> GetByIdAsync(Guid id)
    {
        var key = await _apiKeyRepo.GetByIdAsync(id);
        if (key == null) throw new CustomHttpBadRequestException("api_key", "API key not found.");
        return MapToResponse(key);
    }

    public async Task<ApiKeyCreatedResponse> CreateAsync(ApiKeyCreateRequest request, string? createdBy)
    {
        var rawKey = GenerateApiKey();
        var keyHash = _passwordService.HashSha256(rawKey);
        var prefix = rawKey[..8];

        var entity = new ApiKey
        {
            TenantId = request.TenantId,
            ClientId = request.ClientId ?? Guid.Empty,
            UserId = request.UserId,
            KeyHash = keyHash,
            KeyPrefix = prefix,
            KeyName = request.Name,
            Scopes = request.Scopes,
            ExpiresAt = request.ExpiresAt,
            IsActive = true
        };
        _apiKeyRepo.Insert(entity);
        await _unitOfWork.SaveChangesAsync();

        var response = MapToCreatedResponse(entity);
        response.RawKey = rawKey;
        return response;
    }

    public async Task<DeleteResponse> RevokeAsync(Guid id, string? revokedBy)
    {
        var key = await _apiKeyRepo.GetByIdAsync(id);
        if (key == null) throw new CustomHttpBadRequestException("api_key", "API key not found.");
        key.RevokedAt = DateTime.UtcNow;
        key.IsActive = false;
        key.DeletedAt = DateTime.UtcNow;
        key.DeletedBy = revokedBy;
        _apiKeyRepo.Update(key);
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = key.Id, IsSuccess = true, Message = "API key revoked." };
    }

    public async Task<UpdateResponse> UpdateLastUsedAsync(Guid id)
    {
        var key = await _apiKeyRepo.GetByIdAsync(id);
        if (key == null) return new UpdateResponse { Id = id, IsSuccess = false };
        key.LastUsedAt = DateTime.UtcNow;
        _apiKeyRepo.Update(key);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = id, IsSuccess = true };
    }

    public async Task<ApiKeyResponse?> ValidateAsync(string rawKey)
    {
        if (string.IsNullOrEmpty(rawKey)) return null;
        var keyHash = _passwordService.HashSha256(rawKey);
        var key = await _apiKeyRepo.GetByKeyHashAsync(keyHash);
        if (key == null || !key.IsActive || key.RevokedAt.HasValue || key.DeletedAt.HasValue) return null;
        if (key.ExpiresAt.HasValue && key.ExpiresAt < DateTime.UtcNow) return null;
        return MapToResponse(key);
    }

    private static ApiKeyResponse MapToResponse(ApiKey k) => new()
    {
        Id = k.Id.ToString(),
        TenantId = k.TenantId?.ToString(),
        ClientId = k.ClientId == Guid.Empty ? null : k.ClientId.ToString(),
        UserId = k.UserId?.ToString(),
        KeyPrefix = k.KeyPrefix,
        Name = k.KeyName,
        Scopes = k.Scopes,
        ExpiresAt = k.ExpiresAt,
        LastUsedAt = k.LastUsedAt,
        IsActive = k.IsActive,
        CreatedAt = k.CreatedAt
    };

    private static ApiKeyCreatedResponse MapToCreatedResponse(ApiKey k) => new()
    {
        Id = k.Id.ToString(),
        TenantId = k.TenantId?.ToString(),
        ClientId = k.ClientId == Guid.Empty ? null : k.ClientId.ToString(),
        UserId = k.UserId?.ToString(),
        KeyPrefix = k.KeyPrefix,
        Name = k.KeyName,
        Scopes = k.Scopes,
        ExpiresAt = k.ExpiresAt,
        IsActive = k.IsActive,
        CreatedAt = k.CreatedAt,
        RawKey = string.Empty  // Will be set by caller
    };

    private static string GenerateApiKey() =>
        "dk_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
