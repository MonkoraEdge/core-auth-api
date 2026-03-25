namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.ScopeAggregate;

public class ScopeCreateRequest
{
    public string ScopeName { get; set; }
    public string? DisplayName { get; set; }
    public string ScopeType { get; set; } = "CUSTOM";
    public string[]? Claims { get; set; }
    public bool IsSystemScope { get; set; }
}

public class ScopeUpdateRequest
{
    public string? DisplayName { get; set; }
    public string[]? Claims { get; set; }
    public bool? IsActive { get; set; }
}

public class ScopeResponse
{
    public string Id { get; set; }
    public string ScopeName { get; set; }
    public string? DisplayName { get; set; }
    public string ScopeType { get; set; }
    public string[]? Claims { get; set; }
    public bool IsSystemScope { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
