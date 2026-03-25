using Microsoft.Extensions.Options;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AuthAggregate
{
    public class ApiKeyAuthenticationPostConfigureOptions : IPostConfigureOptions<ApiKeyAuthenticationOptions>
    {
        public void PostConfigure(string? name, ApiKeyAuthenticationOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ApiKeys))
                throw new InvalidOperationException("ApiKeys must be provided in options.");
        }
    }
}