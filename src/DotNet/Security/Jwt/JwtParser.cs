using System.IdentityModel.Tokens.Jwt;

namespace MonkoraEdge.Core.DotNet.Security.Jwt
{
    //For Auth
    public static class JwtParser
    {
        public static IDictionary<string, object> ParseClaims(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            return jwt.Payload;
        }
    }
}
