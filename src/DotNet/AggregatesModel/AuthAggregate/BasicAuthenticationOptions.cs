using Microsoft.AspNetCore.Authentication;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AuthAggregate
{
    public class BasicAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}