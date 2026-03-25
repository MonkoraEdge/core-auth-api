using Microsoft.Extensions.Options;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AuthAggregate
{
    public class BasicAuthenticationPostConfigureOptions : IPostConfigureOptions<BasicAuthenticationOptions>
    {
        public void PostConfigure(string? name, BasicAuthenticationOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Username) ||
                string.IsNullOrWhiteSpace(options.Password))
                throw new InvalidOperationException("Username and Password must be provided in options.");
        }
    }
}