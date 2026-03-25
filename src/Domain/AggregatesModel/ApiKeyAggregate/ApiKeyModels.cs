namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate;

public class ApiKeyCreateRequest
{
    public Guid? TenantId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; }
    public string[]? Scopes { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ApiKeyResponse
{
    public string Id { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? UserId { get; set; }
    public string KeyPrefix { get; set; }
    public string Name { get; set; }
    public string[]? Scopes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApiKeyCreatedResponse : ApiKeyResponse
{
    public string RawKey { get; set; }  // only returned once on creation
}
