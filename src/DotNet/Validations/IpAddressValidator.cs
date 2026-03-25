using System.Net;

namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class IpAddressValidator
    {
        public static bool IsValid(string ip)
            => IPAddress.TryParse(ip, out _);
    }
}
