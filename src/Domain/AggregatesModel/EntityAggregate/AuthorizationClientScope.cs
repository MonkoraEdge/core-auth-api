using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class AuthorizationClientScope : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid ScopeId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsRequired { get; set; }
}
