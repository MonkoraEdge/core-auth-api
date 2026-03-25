using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class AuditLog : BaseEntity
{
    public Guid? ClientId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }

    public string ActorType { get; set; }
    public string Action { get; set; }
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Result { get; set; }

    public string? Metadata { get; set; }
}
