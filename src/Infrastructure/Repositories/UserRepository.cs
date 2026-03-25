using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class UserRepository : AuthRepositoryBase<User>, IUserRepository
{
    public UserRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<User?> GetByIdAsync(Guid id) =>
        await Context.Users.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await Context.Users.FirstOrDefaultAsync(m => m.Email == email);

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber) =>
        await Context.Users.FirstOrDefaultAsync(m => m.PhoneNumber == phoneNumber);
}
