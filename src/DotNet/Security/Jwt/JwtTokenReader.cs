using System.IdentityModel.Tokens.Jwt;

namespace MonkoraEdge.Core.DotNet.Security.Jwt
{
    public static class JwtTokenReader
    {
        public static string? GetClaim(string token, string claimType)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            return jwt.Claims.FirstOrDefault(x => x.Type == claimType)?.Value;
        }
    }
}
