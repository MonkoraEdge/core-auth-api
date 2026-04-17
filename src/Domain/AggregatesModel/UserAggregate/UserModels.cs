namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate;

public class UserCreateRequest
{
    public string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FirstNameEn { get; set; }
    public string? LastNameEn { get; set; }
    public string? DisplayName { get; set; }
    public string? Password { get; set; }
    public string? Locale { get; set; }
    public string? Zoneinfo { get; set; }
    public Guid? TenantId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UserUpdateRequest
{
    public string? PhoneNumber { get; set; }
    public string? FirstNameEn { get; set; }
    public string? LastNameEn { get; set; }
    public string? DisplayName { get; set; }
    public string? Locale { get; set; }
    public string? Zoneinfo { get; set; }
    public bool? IsActive { get; set; }
}

public class UserResponse
{
    public string Id { get; set; }
    public string? TenantId { get; set; }
    public string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Locale { get; set; }
    public string? Zoneinfo { get; set; }
    public string? AvatarUrl { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public string Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserDataSourceRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public Guid? TenantId { get; set; }
    public bool? IsActive { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; } = "created_at";
    public string? SortDir { get; set; } = "desc";
}

public class AssignRoleRequest
{
    public List<Guid> RoleIds { get; set; } = new();
    public Guid? TenantId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UserSessionResponse
{
    public string Id { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDeviceResponse
{
    public string Id { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public string? BrowserName { get; set; }
    public string? BrowserVersion { get; set; }
    public string? IpAddress { get; set; }
    public string? LoginMethod { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsActive { get; set; }
}
