using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace MonkoraEdge.Core.Auth.Infrastructure.Configurations;

[ExcludeFromCodeCoverage]
public class EnvironmentOptions
{
    [Required] public string POSTGRES_CONNECTIONSTRING { get; init; }
    [Required] public string BASIC_AUTHENTICATION_USERNAME { get; init; }
    [Required] public string BASIC_AUTHENTICATION_PASSWORD { get; init; } 
    
    [Required] public int TOKEN_EXPIRES_IN_MINUTES { get; init; }
    [Required] public string AUTH_ISSUER { get; init; }
    [Required] public string OAUTH2_SIGNED_PRIVATE_KEY { get; init; }
    [Required] public string REDIS_CONNECTIONSTRING { get; init; }
    [Required] public string API_KEYS { get; init; }
    [Required] public string GOOGLE_API_AUTH_KEY { get; init; }
    [Required] public string GOOGLE_API_AUTH_ENDPOINT { get; init; }
    [Required] public string GOOGLE_OAUTH2_API_AUTH_ENDPOINT { get; init; }
    [Required] public string GOOGLE_APPLICATION_CREDENTIALS_AUTH { get; init; }
    [Required] public string ARGON2_SECRET { get; init; }
    [Required] public string NOTIFICATION_ENDPOINT { get; init; }
    [Required] public string BP_API_ENDPOINT { get; init; }
    [Required] public int SIGNIN_FAILED_IN_MINUTES { get; init; }
    [Required] public int BLOCK_IP_ADDRESS_IN_MINUTES { get; init; }
    [Required] public string RESOURCE_API_ENDPOINT { get; init; }
    [Required] public string FACEBOOK_API_ENDPOINT { get; init; }
    [Required] public string APPLE_API_ENDPOINT { get; init; }

    [Required] public string AUTH_JWKS_ENDPOINT { get; init; }
    [Required] public string ACCOUNT_TOYO_ENDPOINT { get; init; }
    [Required] public string CLIENT_TOYO_FORGOT_PASSWORD_ENDPOINT { get; init; }

    [Required] public string ACCOUNT_DELETION_GRACE_PERIOD_DAYS { get; init; }
    
    [Required] public string? AMQP_HOST { get; set; }
    [Required] public string? AMQP_PORT { get; set; }
    [Required] public string? AMQP_USERNAME { get; set; }
    [Required] public string? AMQP_PASSWORD { get; set; }
    [Required] public string? AMQP_SYNC_DELETE_ACCOUNT_EXCHANGE_KEY { get; set; }
    [Required] public string HASH_SECRET_KEY { get; init; }
    [Required] public string HASH_SECRET_IV { get; init; }
}