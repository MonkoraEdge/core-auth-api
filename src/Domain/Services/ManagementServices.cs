using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ScopeAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
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
        if (scope == null) throw new DomainException("scope", "Scope not found.");
        return MapToResponse(scope);
    }

    public async Task<CreateResponse> CreateAsync(ScopeCreateRequest request, string? createdBy)
    {
        var existing = await _scopeRepo.GetByScopeNameAsync(request.ScopeName);
        if (existing != null) throw new DomainException("scope", $"Scope '{request.ScopeName}' already exists.");

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
        if (scope == null) throw new DomainException("scope", "Scope not found.");
        if (scope.IsSystemScope) throw new DomainException("scope", "System scopes cannot be modified.");
        if (request.Claims != null) scope.Claims = request.Claims;
        if (request.IsActive.HasValue) scope.IsActive = request.IsActive.Value;
        _scopeRepo.Update(scope);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = scope.Id, IsSuccess = true, Message = "Scope updated." };
    }

    public async Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy)
    {
        var scope = await _scopeRepo.GetByIdAsync(id);
        if (scope == null) throw new DomainException("scope", "Scope not found.");
        if (scope.IsSystemScope) throw new DomainException("scope", "System scopes cannot be deleted.");
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
        if (role == null) throw new DomainException("role", "Role not found.");
        return await MapToResponseAsync(role);
    }

    public async Task<CreateResponse> CreateAsync(RoleCreateRequest request, string? createdBy)
    {
        var existing = await _roleRepo.GetByRoleCodeAsync(request.RoleCode, request.TenantId);
        if (existing != null) throw new DomainException("role", $"Role code '{request.RoleCode}' already exists.");

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
        if (role == null) throw new DomainException("role", "Role not found.");
        if (request.IsActive.HasValue) role.IsActive = request.IsActive.Value;
        _roleRepo.Update(role);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = role.Id, IsSuccess = true, Message = "Role updated." };
    }

    public async Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy)
    {
        var role = await _roleRepo.GetByIdAsync(id);
        if (role == null) throw new DomainException("role", "Role not found.");
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
        if (role == null) throw new DomainException("role", "Role not found.");

        var existing = await _rolePermissionRepo.GetByRoleIdAsync(roleId);

        foreach (var permId in request.PermissionIds)
        {
            if (existing.Any(rp => rp.PermissionId == permId)) continue;
            var perm = await _permissionRepo.GetByIdAsync(permId);
            if (perm == null) throw new DomainException("role", $"Permission {permId} not found.");
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
        if (perm == null) throw new DomainException("permission", "Permission not found.");
        return MapToResponse(perm);
    }

    public async Task<CreateResponse> CreateAsync(PermissionCreateRequest request, string? createdBy)
    {
        var existing = await _permissionRepo.GetByPermissionCodeAsync(request.PermissionCode, request.TenantId);
        if (existing != null) throw new DomainException("permission", $"Permission code '{request.PermissionCode}' already exists.");

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
        if (perm == null) throw new DomainException("permission", "Permission not found.");
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
        if (perm == null) throw new DomainException("permission", "Permission not found.");
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
        if (key == null) throw new DomainException("api_key", "API key not found.");
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
        if (key == null) throw new DomainException("api_key", "API key not found.");
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

// ─── ProviderService ──────────────────────────────────────────────────────────

public class ProviderService : IProviderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProviderRepository _providerRepo;

    public ProviderService(IUnitOfWork unitOfWork, IProviderRepository providerRepo)
    {
        _unitOfWork = unitOfWork;
        _providerRepo = providerRepo;
    }

    public async Task<List<ProviderResponse>> GetAllAsync(bool? activeOnly = true)
    {
        var providers = activeOnly == true
            ? await _providerRepo.GetAllActiveAsync()
            : await _providerRepo.ListAsync();
        return providers.Select(MapToResponse).ToList();
    }

    public async Task<ProviderResponse> GetByIdAsync(Guid id)
    {
        var provider = await _providerRepo.GetByIdAsync(id);
        if (provider == null) throw new DomainException("provider", "Provider not found.");
        return MapToResponse(provider);
    }

    public async Task<CreateResponse> CreateAsync(ProviderCreateRequest request, string? createdBy)
    {
        var existing = await _providerRepo.GetByProviderCodeAsync(request.ProviderCode);
        if (existing != null)
            throw new DomainException("provider", $"Provider code '{request.ProviderCode}' already exists.");

        var provider = new Provider
        {
            ProviderCode = request.ProviderCode.ToUpperInvariant().Trim(),
            ProviderName = request.ProviderName,
            Protocol = request.Protocol,
            ClientId = request.ClientId,
            Scopes = request.Scopes,
            Issuer = request.Issuer,
            AuthorizationUrl = request.AuthorizationUrl,
            TokenUrl = request.TokenUrl,
            UserinfoUrl = request.UserinfoUrl,
            JwksUri = request.JwksUri,
            DiscoveryUrl = request.DiscoveryUrl,
            EndSessionEndpoint = request.EndSessionEndpoint,
            CallbackUrl = request.CallbackUrl,
            PkceSupported = request.PkceSupported,
            IsActive = request.IsActive
        };
        _providerRepo.Insert(provider);
        await _unitOfWork.SaveChangesAsync();
        return new CreateResponse { Id = provider.Id, IsSuccess = true, Message = "Provider created." };
    }

    public async Task<UpdateResponse> UpdateAsync(Guid id, ProviderUpdateRequest request, string? updatedBy)
    {
        var provider = await _providerRepo.GetByIdAsync(id);
        if (provider == null) throw new DomainException("provider", "Provider not found.");

        if (request.ProviderName != null) provider.ProviderName = request.ProviderName;
        if (request.ClientId != null) provider.ClientId = request.ClientId;
        if (request.Scopes != null) provider.Scopes = request.Scopes;
        if (request.AuthorizationUrl != null) provider.AuthorizationUrl = request.AuthorizationUrl;
        if (request.TokenUrl != null) provider.TokenUrl = request.TokenUrl;
        if (request.UserinfoUrl != null) provider.UserinfoUrl = request.UserinfoUrl;
        if (request.JwksUri != null) provider.JwksUri = request.JwksUri;
        if (request.DiscoveryUrl != null) provider.DiscoveryUrl = request.DiscoveryUrl;
        if (request.EndSessionEndpoint != null) provider.EndSessionEndpoint = request.EndSessionEndpoint;
        if (request.CallbackUrl != null) provider.CallbackUrl = request.CallbackUrl;
        if (request.PkceSupported.HasValue) provider.PkceSupported = request.PkceSupported.Value;
        if (request.IsActive.HasValue) provider.IsActive = request.IsActive.Value;

        _providerRepo.Update(provider);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = provider.Id, IsSuccess = true, Message = "Provider updated." };
    }

    public async Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy)
    {
        var provider = await _providerRepo.GetByIdAsync(id);
        if (provider == null) throw new DomainException("provider", "Provider not found.");
        provider.DeletedAt = DateTime.UtcNow;
        provider.DeletedBy = deletedBy;
        provider.IsActive = false;
        _providerRepo.Update(provider);
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = provider.Id, IsSuccess = true, Message = "Provider deleted." };
    }

    private static ProviderResponse MapToResponse(Provider p) => new()
    {
        Id = p.Id.ToString(),
        ProviderCode = p.ProviderCode,
        ProviderName = p.ProviderName,
        Protocol = p.Protocol,
        ClientId = p.ClientId,
        Scopes = p.Scopes,
        Issuer = p.Issuer,
        AuthorizationUrl = p.AuthorizationUrl,
        TokenUrl = p.TokenUrl,
        UserinfoUrl = p.UserinfoUrl,
        JwksUri = p.JwksUri,
        DiscoveryUrl = p.DiscoveryUrl,
        EndSessionEndpoint = p.EndSessionEndpoint,
        CallbackUrl = p.CallbackUrl,
        PkceSupported = p.PkceSupported,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };
}

// ─── AgreementService ─────────────────────────────────────────────────────────

public class AgreementService : IAgreementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAgreementRepository _agreementRepo;

    public AgreementService(IUnitOfWork unitOfWork, IAgreementRepository agreementRepo)
    {
        _unitOfWork = unitOfWork;
        _agreementRepo = agreementRepo;
    }

    public async Task<List<AgreementResponse>> GetListAsync(Guid? tenantId, bool? activeOnly = true)
    {
        var agreements = tenantId.HasValue
            ? await _agreementRepo.GetActiveByTenantAsync(tenantId.Value)
            : await _agreementRepo.GetActiveByTenantAsync(null);

        var list = agreements.AsEnumerable();
        if (activeOnly == true) list = list.Where(a => a.IsActive);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<AgreementResponse> GetByIdAsync(Guid id)
    {
        var agreement = await _agreementRepo.GetByIdAsync(id);
        if (agreement == null) throw new DomainException("agreement", "Agreement not found.");
        return MapToResponse(agreement);
    }

    public async Task<CreateResponse> CreateAsync(AgreementCreateRequest request, string? createdBy)
    {
        var existing = await _agreementRepo.GetByAgreementCodeAsync(request.AgreementCode);
        if (existing != null)
            throw new DomainException("agreement", $"Agreement code '{request.AgreementCode}' already exists.");

        var agreement = new Agreement
        {
            TenantId = request.TenantId,
            AgreementCode = request.AgreementCode.ToUpperInvariant().Trim(),
            AgreementType = request.AgreementType.ToUpperInvariant(),
            Title = request.Title,
            Content = request.Content,
            Summary = request.Summary,
            Version = request.Version,
            EffectiveAt = request.EffectiveAt,
            ExpiresAt = request.ExpiresAt,
            IsRequired = request.IsRequired,
            RequiresExplicitAction = request.RequiresExplicitAction,
            IsActive = request.IsActive
        };
        _agreementRepo.Insert(agreement);
        await _unitOfWork.SaveChangesAsync();
        return new CreateResponse { Id = agreement.Id, IsSuccess = true, Message = "Agreement created." };
    }

    public async Task<UpdateResponse> UpdateAsync(Guid id, AgreementUpdateRequest request, string? updatedBy)
    {
        var agreement = await _agreementRepo.GetByIdAsync(id);
        if (agreement == null) throw new DomainException("agreement", "Agreement not found.");

        if (request.Title != null) agreement.Title = request.Title;
        if (request.Content != null) agreement.Content = request.Content;
        if (request.Summary != null) agreement.Summary = request.Summary;
        if (request.Version != null) agreement.Version = request.Version;
        if (request.EffectiveAt.HasValue) agreement.EffectiveAt = request.EffectiveAt.Value;
        if (request.ExpiresAt.HasValue) agreement.ExpiresAt = request.ExpiresAt;
        if (request.IsRequired.HasValue) agreement.IsRequired = request.IsRequired.Value;
        if (request.RequiresExplicitAction.HasValue) agreement.RequiresExplicitAction = request.RequiresExplicitAction.Value;
        if (request.IsActive.HasValue) agreement.IsActive = request.IsActive.Value;

        _agreementRepo.Update(agreement);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = agreement.Id, IsSuccess = true, Message = "Agreement updated." };
    }

    public async Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy)
    {
        var agreement = await _agreementRepo.GetByIdAsync(id);
        if (agreement == null) throw new DomainException("agreement", "Agreement not found.");
        agreement.DeletedAt = DateTime.UtcNow;
        agreement.DeletedBy = deletedBy;
        agreement.IsActive = false;
        _agreementRepo.Update(agreement);
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = agreement.Id, IsSuccess = true, Message = "Agreement deleted." };
    }

    private static AgreementResponse MapToResponse(Agreement a) => new()
    {
        Id = a.Id.ToString(),
        TenantId = a.TenantId?.ToString(),
        AgreementCode = a.AgreementCode,
        AgreementType = a.AgreementType,
        Title = a.Title,
        Content = a.Content,
        Summary = a.Summary,
        Version = a.Version,
        EffectiveAt = a.EffectiveAt,
        ExpiresAt = a.ExpiresAt,
        IsRequired = a.IsRequired,
        RequiresExplicitAction = a.RequiresExplicitAction,
        IsActive = a.IsActive,
        CreatedAt = a.CreatedAt
    };
}
