using Microsoft.AspNetCore.Authentication;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AuthAggregate
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string ApiKeys { get; set; }
    }
}