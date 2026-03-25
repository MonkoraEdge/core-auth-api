using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class UserFile : BaseEntity, ISoftDelete
{
    public Guid UserId { get; set; }

    public string? FileMimeType { get; set; }
    public Locale FileName { get; set; }
    public string FileUrl { get; set; }
    public string FileType { get; set; } = "OTHER";
    public int? FileSize { get; set; }

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
