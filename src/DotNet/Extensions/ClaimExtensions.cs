using System.Security.Claims;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    //API/Application
    public static class ClaimExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user)
            => user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public static string? GetEmail(this ClaimsPrincipal user)
            => user?.FindFirst(ClaimTypes.Email)?.Value;

        public static string? GetRole(this ClaimsPrincipal user)
            => user?.FindFirst(ClaimTypes.Role)?.Value;

        public static bool HasPermission(this ClaimsPrincipal user, string permission)
            => user?.Claims.Any(c => c.Type == "permission" && c.Value == permission) ?? false;
    }
}
