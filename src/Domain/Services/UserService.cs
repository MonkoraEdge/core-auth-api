using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepo;
    private readonly IUserIdentityRepository _identityRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IPasswordService _passwordService;

    public UserService(
        IUnitOfWork unitOfWork,
        IUserRepository userRepo,
        IUserIdentityRepository identityRepo,
        IUserRoleRepository userRoleRepo,
        IRoleRepository roleRepo,
        IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _userRepo = userRepo;
        _identityRepo = identityRepo;
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
        _passwordService = passwordService;
    }

    public async Task<UserResponse> GetByIdAsync(Guid id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) throw new DomainException("user", "User not found.");
        return await MapToResponseAsync(user);
    }

    public async Task<(List<UserResponse> Items, int Total)> GetListAsync(UserDataSourceRequest request)
    {
        var query = _userRepo.ListAsync(u =>
            (request.TenantId == null || u.TenantId == request.TenantId) &&
            (request.IsActive == null || u.IsActive == request.IsActive.Value) &&
            (request.Status == null || u.Status == request.Status) &&
            (string.IsNullOrEmpty(request.Search) ||
                u.Email.Contains(request.Search) ||
                (u.DisplayName != null && u.DisplayName.Contains(request.Search))) &&
            u.DeletedAt == null);

        var all = await query;
        var sorted = request.SortDir?.ToLower() == "asc"
            ? (IEnumerable<User>)all.OrderBy(u => u.CreatedAt)
            : all.OrderByDescending(u => u.CreatedAt);

        var total = sorted.Count();
        var skip = (request.Page - 1) * request.PageSize;
        var page = sorted.Skip(skip).Take(request.PageSize).ToList();

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
}
