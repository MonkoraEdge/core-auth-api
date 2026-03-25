using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace MonkoraEdge.Core.DotNet.Security.Jwt
{
    //For API Gateway
    public static class JwtValidator
    {
        public static bool Validate(string token, string secret, bool validateLifetime = true)
        {
            var handler = new JwtSecurityTokenHandler();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = validateLifetime
            };

            try
            {
                handler.ValidateToken(token, parameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
