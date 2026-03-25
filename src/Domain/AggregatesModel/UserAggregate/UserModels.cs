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
