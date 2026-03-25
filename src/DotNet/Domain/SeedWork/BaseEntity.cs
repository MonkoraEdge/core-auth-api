using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.Interfaces.DomainEvent;

namespace MonkoraEdge.Core.DotNet.Domain.SeedWork
{
    public abstract class BaseEntity : IEntity
    {
        private int? _requestedHashCode;
        private List<IDomainEvent> _domainEvents;

        /// <summary>Unique entity identifier. Auto-generated on construction.</summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        // ----------------------------------------------------------------
        // Domain Events
        // ----------------------------------------------------------------

        /// <summary>
        /// Domain events raised by this entity since the last save.
        /// Events are dispatched by <c>BaseDbContext</c> after a successful <c>SaveChangesAsync</c>.
        /// </summary>
        public IReadOnlyList<IDomainEvent> DomainEvents =>
            _domainEvents?.AsReadOnly() ?? (IReadOnlyList<IDomainEvent>)Array.Empty<IDomainEvent>();

        /// <summary>Add a domain event to be dispatched after the next successful save.</summary>
        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents ??= new List<IDomainEvent>();
            _domainEvents.Add(domainEvent);
        }

        /// <summary>Remove a specific domain event (e.g. when the action was undone before save).</summary>
        public void RemoveDomainEvent(IDomainEvent domainEvent)
            => _domainEvents?.Remove(domainEvent);

        /// <summary>Clear all pending domain events (called by <c>BaseDbContext</c> after dispatch).</summary>
        public void ClearDomainEvents()
            => _domainEvents?.Clear();

        public override bool Equals(object? obj)
        {
            if (!(obj is BaseEntity))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (GetType() != obj.GetType())
                return false;

            var item = (BaseEntity)obj;

            if (item.IsTransient() || IsTransient())
                return false;

            return item.Id == Id;
        }

        public override int GetHashCode()
        {
            if (!IsTransient())
            {
                if (!_requestedHashCode.HasValue)
                    _requestedHashCode = Id.GetHashCode() ^ 31;

                return _requestedHashCode.Value;
            }

            return base.GetHashCode();
        }

        public bool IsTransient()
        {
            return Id == default;
        }

        public static bool operator ==(BaseEntity left, BaseEntity right)
        {
            if (Equals(left, null))
                return Equals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(BaseEntity left, BaseEntity right)
        {
            return !(left == right);
        }
    }
}
