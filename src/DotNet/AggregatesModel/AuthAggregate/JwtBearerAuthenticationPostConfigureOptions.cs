using Microsoft.Extensions.Options;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AuthAggregate
{

    public class JwtBearerAuthenticationPostConfigureOptions : IPostConfigureOptions<JwtBearerAuthenticationOptions>
    {
        public void PostConfigure(string? name, JwtBearerAuthenticationOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.JWKsEndpoint))
                throw new InvalidOperationException("JWKsEndpoint must be provided in options.");
        }
    }
}