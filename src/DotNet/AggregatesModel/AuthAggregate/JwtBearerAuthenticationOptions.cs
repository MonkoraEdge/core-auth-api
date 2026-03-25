using Microsoft.AspNetCore.Authentication;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AuthAggregate
{
    public class JwtBearerAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string JWKsEndpoint { get; set; }
    }
}