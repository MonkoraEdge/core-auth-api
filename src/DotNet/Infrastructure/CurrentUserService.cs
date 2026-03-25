using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace MonkoraEdge.Core.DotNet.Infrastructure
{
    /// <summary>
    /// Default implementation of ICurrentUserService backed by IHttpContextAccessor.
    /// Resolves user identity from the current HTTP request JWT claims.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public string UserId =>
            Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Principal?.FindFirstValue("sub");

        public string UserName =>
            Principal?.FindFirstValue(ClaimTypes.Name)
            ?? Principal?.FindFirstValue("name");

        public string Email =>
            Principal?.FindFirstValue(ClaimTypes.Email)
            ?? Principal?.FindFirstValue("email");

        public string Role =>
            Principal?.FindFirstValue(ClaimTypes.Role)
            ?? Principal?.FindFirstValue("role");

        public IEnumerable<Claim> Claims =>
            Principal?.Claims ?? Enumerable.Empty<Claim>();
    }
}
