using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class AuthorizationCodeScope : BaseEntity
{
    public Guid AuthorizationCodeId { get; set; }
    public Guid ScopeId { get; set; }
}
