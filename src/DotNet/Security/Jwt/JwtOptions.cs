using System.ComponentModel.DataAnnotations;

namespace MonkoraEdge.Core.DotNet.Security.Jwt
{
    /// <summary>
    /// JWT authentication options — bind from <c>appsettings.json</c> section <c>"Jwt"</c>
    /// and register via <c>services.AddJwtAuthentication(configuration)</c>.
    ///
    /// Example appsettings.json:
    /// <code>
    /// "Jwt": {
    ///   "Secret": "your-256-bit-secret-key-here",
    ///   "Issuer": "https://your-domain.com",
    ///   "Audience": "your-api",
    ///   "ExpiryMinutes": 60,
    ///   "RefreshTokenExpiryDays": 7
    /// }
    /// </code>
    /// </summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        /// <summary>Symmetric signing secret (min. 32 chars for HS256).</summary>
        [Required]
        public string Secret { get; set; }

        /// <summary>Token issuer — set to your API or identity server URL.</summary>
        public string Issuer { get; set; }

        /// <summary>Intended audience (e.g. your API identifier).</summary>
        public string Audience { get; set; }

        /// <summary>Access token expiry in minutes. Default: 60.</summary>
        public int ExpiryMinutes { get; set; } = 60;

        /// <summary>Refresh token expiry in days. Default: 7.</summary>
        public int RefreshTokenExpiryDays { get; set; } = 7;

        /// <summary>Whether to validate the token issuer. Default: true.</summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>Whether to validate the audience claim. Default: true.</summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>Whether to validate the token lifetime (expiry). Default: true.</summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>Clock skew applied to token expiry validation. Default: 0 seconds (strict).</summary>
        public int ClockSkewSeconds { get; set; } = 0;
    }
}
