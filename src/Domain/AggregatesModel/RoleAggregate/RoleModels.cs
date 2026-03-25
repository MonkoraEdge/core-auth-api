namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate;

public class RoleCreateRequest
{
    public string RoleCode { get; set; }
    public string RoleNameEn { get; set; }
    public Guid? TenantId { get; set; }
}

public class RoleUpdateRequest
{
    public string? RoleNameEn { get; set; }
    public bool? IsActive { get; set; }
}

public class RoleResponse
{
    public string Id { get; set; }
    public string? TenantId { get; set; }
    public string RoleCode { get; set; }
    public string RoleName { get; set; }
    public bool IsActive { get; set; }
    public List<PermissionResponse> Permissions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class PermissionCreateRequest
{
    public string PermissionCode { get; set; }
    public string PermissionNameEn { get; set; }
    public string Resource { get; set; }
    public string Action { get; set; }
    public Guid? TenantId { get; set; }
}

public class PermissionUpdateRequest
{
    public string? PermissionNameEn { get; set; }
    public string? Resource { get; set; }
    public string? Action { get; set; }
    public bool? IsActive { get; set; }
}

public class PermissionResponse
{
    public string Id { get; set; }
    public string PermissionCode { get; set; }
    public string PermissionName { get; set; }
    public string Resource { get; set; }
    public string Action { get; set; }
    public bool IsActive { get; set; }
}

public class AssignPermissionRequest
{
    public List<Guid> PermissionIds { get; set; } = new();
}
