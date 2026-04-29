using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using System.Linq.Expressions;

namespace MonkoraEdge.Core.Auth.Domain.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepo;
    private readonly IUserIdentityRepository _identityRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IPasswordService _passwordService;
    private readonly IUserSessionRepository _sessionRepo;
    private readonly IUserSessionDeviceRepository _deviceRepo;

    public UserService(
        IUnitOfWork unitOfWork,
        IUserRepository userRepo,
        IUserIdentityRepository identityRepo,
        IUserRoleRepository userRoleRepo,
        IRoleRepository roleRepo,
        IPasswordService passwordService,
        IUserSessionRepository sessionRepo,
        IUserSessionDeviceRepository deviceRepo)
    {
        _unitOfWork = unitOfWork;
        _userRepo = userRepo;
        _identityRepo = identityRepo;
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
        _passwordService = passwordService;
        _sessionRepo = sessionRepo;
        _deviceRepo = deviceRepo;
    }

    public async Task<UserResponse> GetByIdAsync(Guid id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) throw new DomainException("user", "User not found.");
        return await MapToResponseAsync(user);
    }

    public async Task<(List<UserResponse> Items, int Total)> GetListAsync(UserDataSourceRequest request)
    {
        Expression<Func<User, bool>> predicate = u =>
            (request.TenantId == null || u.TenantId == request.TenantId) &&
            (request.IsActive == null || u.IsActive == request.IsActive.Value) &&
            (request.Status == null || u.Status == request.Status) &&
            (string.IsNullOrEmpty(request.Search) ||
                u.Email.Contains(request.Search) ||
                (u.DisplayName != null && u.DisplayName.Contains(request.Search))) &&
            u.DeletedAt == null;

        bool ascending = request.SortDir?.ToLower() == "asc";
        var (page, total) = await _userRepo.GetPagedAsync(predicate, request.Page, request.PageSize, ascending);

        var responses = new List<UserResponse>();
        foreach (var u in page)
            responses.Add(await MapToResponseAsync(u));

        return (responses, total);
    }

    public async Task<CreateResponse> CreateAsync(UserCreateRequest request, string? createdBy)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email.ToLowerInvariant());
        if (existing != null)
            throw new DomainException("user", "A user with this email already exists.");

        if (!string.IsNullOrEmpty(request.PhoneNumber) && !System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^\+[1-9]\d{1,14}$"))
            throw new DomainException("user", "Phone number must be in E.164 format (e.g., +66812345678).");

        var user = new User
        {
            TenantId = request.TenantId,
            Email = request.Email.ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber,
            DisplayName = request.DisplayName ?? request.Email,
            LocaleCode = request.Locale ?? "en",
            Zoneinfo = request.Zoneinfo,
            Status = "ACTIVE",
            RegistrationSource = "ADMIN",
            IsActive = request.IsActive,
            EmailVerified = false
        };
        _userRepo.Insert(user);

        if (!string.IsNullOrEmpty(request.Password))
        {
            if (!_passwordService.MeetsPasswordPolicy(request.Password))
                throw new DomainException("user", "Password does not meet policy requirements.");

            _identityRepo.Insert(new UserIdentity
            {
                UserId = user.Id,
                ProviderType = "LOCAL",
                Username = user.Email,
                PasswordHash = _passwordService.HashPassword(request.Password),
                PasswordAlgo = "BCRYPT",
                PasswordUpdatedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return new CreateResponse { Id = user.Id, IsSuccess = true, Message = "User created." };
    }

    public async Task<UpdateResponse> UpdateAsync(Guid id, UserUpdateRequest request, string? updatedBy)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) throw new DomainException("user", "User not found.");

        if (request.PhoneNumber != null && !System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^\+[1-9]\d{1,14}$"))
            throw new DomainException("user", "Phone number must be in E.164 format (e.g., +66812345678).");

        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
        if (request.DisplayName != null) user.DisplayName = request.DisplayName;
        if (request.Locale != null) user.LocaleCode = request.Locale;
        if (request.Zoneinfo != null) user.Zoneinfo = request.Zoneinfo;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        _userRepo.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = user.Id, IsSuccess = true, Message = "User updated." };
    }

    public async Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) throw new DomainException("user", "User not found.");

        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = deletedBy;
        user.IsActive = false;
        _userRepo.Update(user);

        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = user.Id, IsSuccess = true, Message = "User deleted." };
    }

    public async Task<UpdateResponse> ActivateAsync(Guid id, string? updatedBy)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) throw new DomainException("user", "User not found.");
        user.Activate();
        _userRepo.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = user.Id, IsSuccess = true, Message = "User activated." };
    }

    public async Task<UpdateResponse> DeactivateAsync(Guid id, string? updatedBy)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) throw new DomainException("user", "User not found.");
        user.Deactivate();
        _userRepo.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = user.Id, IsSuccess = true, Message = "User deactivated." };
    }

    public async Task<UpdateResponse> AssignRolesAsync(Guid userId, AssignRoleRequest request, string? updatedBy)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) throw new DomainException("user", "User not found.");

        var existing = await _userRoleRepo.GetByUserIdAsync(userId);

        foreach (var roleId in request.RoleIds)
        {
            if (existing.Any(ur => ur.RoleId == roleId)) continue;

            var role = await _roleRepo.GetByIdAsync(roleId);
            if (role == null) throw new DomainException("user", $"Role {roleId} not found.");

            _userRoleRepo.Insert(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                ExpiresAt = request.ExpiresAt
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = userId, IsSuccess = true, Message = "Roles assigned." };
    }

    public async Task<UpdateResponse> RemoveRolesAsync(Guid userId, List<Guid> roleIds, string? updatedBy)
    {
        var existing = await _userRoleRepo.GetByUserIdAsync(userId);
        var toRemove = existing.Where(ur => roleIds.Contains(ur.RoleId)).ToList();
        if (toRemove.Any()) _userRoleRepo.DeleteRange(toRemove.ToList());
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = userId, IsSuccess = true, Message = "Roles removed." };
    }

    public async Task<List<UserResponse>> GetByTenantIdAsync(Guid tenantId)
    {
        var users = await _userRepo.ListAsync(u => u.TenantId == tenantId && u.DeletedAt == null);
        var result = new List<UserResponse>();
        foreach (var u in users)
            result.Add(await MapToResponseAsync(u));
        return result;
    }

    private async Task<UserResponse> MapToResponseAsync(User user)
    {
        var userRoles = await _userRoleRepo.GetByUserIdAsync(user.Id);
        var roleNames = new List<string>();
        foreach (var ur in userRoles)
        {
            var role = await _roleRepo.GetByIdAsync(ur.RoleId);
            if (role != null) roleNames.Add(role.RoleCode);
        }

        return new UserResponse
        {
            Id = user.Id.ToString(),
            TenantId = user.TenantId?.ToString(),
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DisplayName = user.DisplayName,
            Locale = user.LocaleCode,
            Zoneinfo = user.Zoneinfo,
            EmailVerified = user.EmailVerified,
            PhoneVerified = user.PhoneVerified,
            Status = user.Status,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = roleNames
        };
    }

    // ── Session management ─────────────────────────────────────────────────────

    public async Task<List<UserSessionResponse>> GetSessionsAsync(Guid userId)
    {
        var sessions = await _sessionRepo.GetActiveByUserIdAsync(userId);
        return sessions.Select(s => new UserSessionResponse
        {
            Id = s.Id.ToString(),
            ClientId = s.ClientId.ToString(),
            DeviceId = s.DeviceId?.ToString(),
            IpAddress = s.IpAddress,
            UserAgent = s.UserAgent,
            ExpiresAt = s.ExpiresAt,
            LastActivityAt = s.LastActivityAt,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task<DeleteResponse> RevokeSessionAsync(Guid userId, Guid sessionId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
            throw new DomainException("session", "Session not found.");

        session.RevokedAt = DateTime.UtcNow;
        session.IsActive = false;
        _sessionRepo.Update(session);
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = sessionId, IsSuccess = true, Message = "Session revoked." };
    }

    public async Task<DeleteResponse> RevokeAllSessionsAsync(Guid userId)
    {
        var sessions = await _sessionRepo.GetActiveByUserIdAsync(userId);
        var now = DateTime.UtcNow;
        foreach (var s in sessions)
        {
            s.RevokedAt = now;
            s.IsActive = false;
            _sessionRepo.Update(s);
        }
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = userId, IsSuccess = true, Message = "All sessions revoked." };
    }

    // ── Device management ──────────────────────────────────────────────────────

    public async Task<List<UserDeviceResponse>> GetDevicesAsync(Guid userId)
    {
        var devices = await _deviceRepo.GetByUserIdAsync(userId);
        return devices.Select(d => new UserDeviceResponse
        {
            Id = d.Id.ToString(),
            DeviceName = d.DeviceName,
            DeviceType = d.DeviceType,
            OsName = d.OsName,
            OsVersion = d.OsVersion,
            BrowserName = d.BrowserName,
            BrowserVersion = d.BrowserVersion,
            IpAddress = d.IpAddress,
            LoginMethod = d.LoginMethod,
            City = d.City,
            Country = d.Country,
            LastSeenAt = d.LastSeenAt,
            LastLoginAt = d.LastLoginAt,
            IsBlocked = d.IsBlocked,
            IsTrusted = d.IsTrusted,
            IsActive = d.IsActive
        }).ToList();
    }

    public async Task<UpdateResponse> TrustDeviceAsync(Guid userId, Guid deviceId)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId);
        if (device == null || device.UserId != userId)
            throw new DomainException("device", "Device not found.");

        device.IsTrusted = true;
        device.IsBlocked = false;
        _deviceRepo.Update(device);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = deviceId, IsSuccess = true, Message = "Device trusted." };
    }

    public async Task<UpdateResponse> BlockDeviceAsync(Guid userId, Guid deviceId)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId);
        if (device == null || device.UserId != userId)
            throw new DomainException("device", "Device not found.");

        device.IsBlocked = true;
        device.IsTrusted = false;
        _deviceRepo.Update(device);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = deviceId, IsSuccess = true, Message = "Device blocked." };
    }

    public async Task<DeleteResponse> RevokeDeviceAsync(Guid userId, Guid deviceId)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId);
        if (device == null || device.UserId != userId)
            throw new DomainException("device", "Device not found.");

        device.RevokedAt = DateTime.UtcNow;
        device.IsActive = false;
        _deviceRepo.Update(device);
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = deviceId, IsSuccess = true, Message = "Device revoked." };
    }
}
