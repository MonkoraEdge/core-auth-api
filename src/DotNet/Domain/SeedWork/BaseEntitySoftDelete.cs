using MonkoraEdge.Core.DotNet.Domain.Interfaces;

namespace MonkoraEdge.Core.DotNet.Domain.SeedWork
{
    public abstract class BaseEntitySoftDelete : BaseEntity, ISoftDelete
    {
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }
}
