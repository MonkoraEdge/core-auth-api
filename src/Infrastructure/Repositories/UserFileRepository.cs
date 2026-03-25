using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class UserFileRepository : BaseRepository<AuthenticationDbContext, UserFile>, IUserFileRepository
{
    public UserFileRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<UserFile?> GetByIdAsync(Guid id) =>
        await Context.UserFiles.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IEnumerable<UserFile>> GetByUserIdAsync(Guid userId) =>
        await Context.UserFiles.Where(m => m.UserId == userId).ToListAsync();

    public async Task<UserFile?> GetActiveProfileByUserIdAsync(Guid userId) =>
        await Context.UserFiles.FirstOrDefaultAsync(m => m.UserId == userId && m.IsActive && m.FileType == "profile");
}
