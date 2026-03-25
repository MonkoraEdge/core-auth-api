using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class PasswordHistory : BaseEntity
{
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; }
    public string? HashAlgorithm { get; set; }
    public int? PasswordStrength { get; set; }
    public bool IsTemporary { get; set; }
}
