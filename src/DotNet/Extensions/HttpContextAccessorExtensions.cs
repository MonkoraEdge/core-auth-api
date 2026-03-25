using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    public static class HttpContextAccessorExtensions
    {
        /// <summary>
        /// Get Authorization from HttpContextAccessor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <returns></returns>
        public static string GetAuthorization(this IHttpContextAccessor httpContextAccessor)
        {
            if (HasAuthorization(httpContextAccessor, out string authorization))
                return authorization;

            throw new AuthenticationException();
        }

        /// <summary>
        /// Get JWT from HttpContextAccessor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <returns></returns>
        public static string GetJwt(this IHttpContextAccessor httpContextAccessor)
        {
            var authorization = GetAuthorization(httpContextAccessor);

            if (string.IsNullOrWhiteSpace(authorization))
                throw new AuthenticationException();

            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            return authorization.Substring("Bearer ".Length).Trim();
        }

        /// <summary>
        /// Get user id from HttpContextAccessor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <returns></returns>
        public static string GetUserId(this IHttpContextAccessor httpContextAccessor)
        {
            var jwt = GetJwt(httpContextAccessor);

            if (string.IsNullOrWhiteSpace(jwt))
                return null;

            var jwtSecurityToken = new JwtSecurityToken(jwt);

            var sub = jwtSecurityToken.Claims.FirstOrDefault(m => m.Type == JwtRegisteredClaimNames.Sub);

            if (sub is null)
                throw new UnauthorizedAccessException();

            return sub.Value;
        }

        /// <summary>
        /// HasAuthorization
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <returns></returns>
        public static bool HasAuthorization(this IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.HasAuthorization(out _);
        }

        /// <summary>
        /// HasAuthorization
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        public static bool HasAuthorization(this IHttpContextAccessor httpContextAccessor, out string authorization)
        {
            authorization = httpContextAccessor.GetHeaderValue(StandardHeader.Authorization);

            return !string.IsNullOrWhiteSpace(authorization);
        }

        /// <summary>
        /// Check Juristic status from HttpContextAccessor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <returns></returns>
        public static bool IsJuristicUser(this IHttpContextAccessor httpContextAccessor)
        {
            if (!HasAuthorization(httpContextAccessor))
                return false;

            var jwt = GetJwt(httpContextAccessor);

            if (string.IsNullOrWhiteSpace(jwt))
                return false;

            var jwtSecurityToken = new JwtSecurityToken(jwt);

            var isJuristic = jwtSecurityToken.Claims.FirstOrDefault(m => m.Type == "isJuristic");

            return isJuristic is not null && Convert.ToBoolean(isJuristic.Value);
        }

        /// <summary>
        /// IsAdminConsole
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <returns></returns>
        public static bool IsAdminConsole(this IHttpContextAccessor httpContextAccessor)
        {
            var jwt = httpContextAccessor.GetJwt();

            if (!httpContextAccessor.HasAuthorization() || string.IsNullOrWhiteSpace(jwt))
                return false;

            var jwtSecurityToken = new JwtSecurityToken(jwt);

            var hasAdminConsole = jwtSecurityToken.Claims
                .FirstOrDefault(m => m.Type == "roles" && m.Value == "AdminConsole");

            return hasAdminConsole is not null;
        }

        public static string GetIpAddress(this IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.GetHeaderValue(StandardHeader.XRealIP);
        }

        public static string GetDeviceId(this IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.GetHeaderValue(StandardHeader.XDeviceId);
        }

        public static string GetPlatform(this IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.GetHeaderValue(StandardHeader.XPlatform);
        }

        public static string GetTraceId(this IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.GetHeaderValue(StandardHeader.XTraceId);
        }

        public static string GetLanguage(this IHttpContextAccessor httpContextAccessor)
        {
            httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Accept-Language", out var acceptLanguageHeaderValue);

            return acceptLanguageHeaderValue;
        }

        /// <summary>
        /// GetHeaderValue
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="headerKey"></param>
        /// <returns></returns>
        private static string GetHeaderValue(this IHttpContextAccessor httpContextAccessor, string headerKey)
        {
            string headerValue = null;

            var headers = httpContextAccessor.HttpContext?.Request.Headers;
            if (headers?.ContainsKey(headerKey) == true)
                headerValue = headers[headerKey];

            return headerValue;
        }
    }
}
