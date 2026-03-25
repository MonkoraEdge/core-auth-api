namespace MonkoraEdge.Core.DotNet.Domain.Interfaces
{
    public interface ISoftDelete
    {
        DateTime? DeletedAt { get; set; }

        string? DeletedBy { get; set; }
    }
}
