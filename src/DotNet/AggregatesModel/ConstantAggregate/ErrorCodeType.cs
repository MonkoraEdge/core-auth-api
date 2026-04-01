using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate
{
    public sealed class ErrorCodeType : Enumeration
    {
        public string Description { get; }
        public string Category { get; }
        public string Severity { get; }

        private ErrorCodeType(
            int value,
            string name,
            string description,
            string category,
            string severity)
            : base(value, name)
        {
            Description = description;
            Category = category;
            Severity = severity;
        }

        private static ErrorCodeType Create(
            int value, string name, string description,
            string category, string severity)
            => new(value, name, description, category, severity);

        // ========================================================================
        // 1000 SYSTEM / INFRASTRUCTURE
        // ========================================================================
        public static readonly ErrorCodeType OPERATION_NOT_SUPPORTED =
            Create(1000, "ERR_OPERATION_NOT_SUPPORTED",
                "The requested operation is not supported.",
                "System", "Error");

        public static readonly ErrorCodeType METHOD_NOT_SUPPORTED =
            Create(1001, "ERR_METHOD_NOT_SUPPORTED",
                "This method is not supported by the system.",
                "System", "Error");

        public static readonly ErrorCodeType FEATURE_NOT_SUPPORTED =
            Create(1002, "ERR_FEATURE_NOT_SUPPORTED",
                "This feature is not supported or implemented.",
                "System", "Error");

        public static readonly ErrorCodeType DATABASE_FAILURE =
            Create(1100, "ERR_DATABASE_FAILURE",
                "Database operation failed.",
                "Infrastructure", "Critical");

        public static readonly ErrorCodeType EXTERNAL_SERVICE_FAILURE =
            Create(1101, "ERR_EXTERNAL_SERVICE_FAILURE",
                "An external dependency or service returned an error.",
                "Infrastructure", "Critical");

        public static readonly ErrorCodeType OPERATION_TIMEOUT =
            Create(1102, "ERR_OPERATION_TIMEOUT",
                "The operation timed out.",
                "Infrastructure", "Warning");

        public static readonly ErrorCodeType CONCURRENCY_CONFLICT =
            Create(1103, "ERR_CONCURRENCY_CONFLICT",
                "A concurrency update conflict occurred.",
                "Infrastructure", "Error");

        public static readonly ErrorCodeType SERIALIZATION_FAILURE =
            Create(1104, "ERR_SERIALIZATION_FAILURE",
                "Failed to serialize or deserialize data.",
                "Infrastructure", "Error");

        public static readonly ErrorCodeType UNEXPECTED =
            Create(1105, "ERR_UNEXPECTED",
                "An unexpected system error occurred.",
                "System", "Critical");

        // ========================================================================
        // 2000 VALIDATION
        // ========================================================================
        public static readonly ErrorCodeType INVALID_INPUT =
            Create(2000, "ERR_INVALID_INPUT",
                "Input is invalid.",
                "Validation", "Error");

        public static readonly ErrorCodeType VALIDATION_FAILED =
            Create(2001, "ERR_VALIDATION_FAILED",
                "Validation failed for one or more fields.",
                "Validation", "Error");

        public static readonly ErrorCodeType MISSING_REQUIRED_FIELD =
            Create(2002, "ERR_MISSING_REQUIRED_FIELD",
                "A required field is missing.",
                "Validation", "Error");

        public static readonly ErrorCodeType INVALID_FORMAT =
            Create(2003, "ERR_INVALID_FORMAT",
                "Input format is invalid.",
                "Validation", "Error");

        // ========================================================================
        // 3000 AUTHENTICATION / AUTHORIZATION
        // ========================================================================
        public static readonly ErrorCodeType UNAUTHORIZED =
            Create(3000, "ERR_UNAUTHORIZED",
                "Authentication failed or missing token.",
                "Auth", "Error");

        public static readonly ErrorCodeType FORBIDDEN =
            Create(3001, "ERR_FORBIDDEN",
                "User is not allowed to access this resource.",
                "Permission", "Error");

        // ========================================================================
        // 3100 OAUTH2.1
        // ========================================================================
        public static readonly ErrorCodeType INVALID_CLIENT =
            Create(3100, "ERR_INVALID_CLIENT",
                "Client authentication failed.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType INVALID_CLIENT_SECRET =
            Create(3101, "ERR_INVALID_CLIENT_SECRET",
                "Client secret is invalid.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType INVALID_GRANT =
            Create(3102, "ERR_INVALID_GRANT",
                "The provided grant is invalid.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType INVALID_TOKEN =
            Create(3103, "ERR_INVALID_TOKEN",
                "The access token is invalid.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType TOKEN_EXPIRED =
            Create(3104, "ERR_TOKEN_EXPIRED",
                "The token has expired.",
                "OAuth2.1", "Warning");

        public static readonly ErrorCodeType TOKEN_REVOKED =
            Create(3105, "ERR_TOKEN_REVOKED",
                "The token has been revoked.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType SCOPE_NOT_ALLOWED =
            Create(3106, "ERR_SCOPE_NOT_ALLOWED",
                "The requested scope is not permitted.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType INVALID_REDIRECT_URI =
            Create(3107, "ERR_INVALID_REDIRECT_URI",
                "The redirect URI is invalid.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType INVALID_PKCE_CODE_VERIFIER =
            Create(3108, "ERR_INVALID_PKCE_CODE_VERIFIER",
                "The PKCE code verifier is invalid.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType INVALID_PKCE_CODE_CHALLENGE =
            Create(3109, "ERR_INVALID_PKCE_CODE_CHALLENGE",
                "The PKCE code challenge is invalid.",
                "OAuth2.1", "Error");

        // NEW STANDARD (Supplemented to fully comply with RFC 6749)
        public static readonly ErrorCodeType INVALID_REQUEST =
            Create(3110, "ERR_INVALID_REQUEST",
                "The request is malformed or missing parameters.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType UNSUPPORTED_GRANT_TYPE =
            Create(3111, "ERR_UNSUPPORTED_GRANT_TYPE",
                "The grant type is not supported.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType INVALID_SCOPE =
            Create(3112, "ERR_INVALID_SCOPE",
                "The requested scope format is invalid.",
                "OAuth2.1", "Error");

        public static readonly ErrorCodeType UNAUTHORIZED_CLIENT =
            Create(3113, "ERR_UNAUTHORIZED_CLIENT",
                "The client is not authorized for the requested grant type.",
                "OAuth2.1", "Error");

        // ========================================================================
        // 4000 API KEY
        // ========================================================================
        public static readonly ErrorCodeType APIKEY_MISSING =
            Create(4000, "ERR_APIKEY_MISSING",
                "API key is missing.",
                "APIKey", "Error");

        public static readonly ErrorCodeType APIKEY_INVALID =
            Create(4001, "ERR_APIKEY_INVALID",
                "API key is invalid.",
                "APIKey", "Error");

        public static readonly ErrorCodeType APIKEY_REVOKED =
            Create(4002, "ERR_APIKEY_REVOKED",
                "API key has been revoked.",
                "APIKey", "Error");

        public static readonly ErrorCodeType APIKEY_EXPIRED =
            Create(4003, "ERR_APIKEY_EXPIRED",
                "API key has expired.",
                "APIKey", "Warning");

        public static readonly ErrorCodeType APIKEY_NOT_AUTHORIZED =
            Create(4004, "ERR_APIKEY_NOT_AUTHORIZED",
                "API key is not authorized for this resource.",
                "APIKey", "Error");

        // ========================================================================
        // 5000 MTLS / CERTIFICATE
        // ========================================================================
        public static readonly ErrorCodeType CLIENT_CERT_MISSING =
            Create(5000, "ERR_CLIENT_CERT_MISSING",
                "Client certificate is missing.",
                "mTLS", "Error");

        public static readonly ErrorCodeType CLIENT_CERT_INVALID =
            Create(5001, "ERR_CLIENT_CERT_INVALID",
                "Client certificate is invalid.",
                "mTLS", "Error");

        public static readonly ErrorCodeType CLIENT_CERT_EXPIRED =
            Create(5002, "ERR_CLIENT_CERT_EXPIRED",
                "Client certificate has expired.",
                "mTLS", "Warning");

        public static readonly ErrorCodeType CLIENT_CERT_NOT_TRUSTED =
            Create(5003, "ERR_CLIENT_CERT_NOT_TRUSTED",
                "Client certificate is not trusted.",
                "mTLS", "Error");

        public static readonly ErrorCodeType CLIENT_CERT_REVOKED =
            Create(5004, "ERR_CLIENT_CERT_REVOKED",
                "Client certificate has been revoked.",
                "mTLS", "Error");

        // ========================================================================
        // 6000 MFA
        // ========================================================================
        public static readonly ErrorCodeType MFA_REQUIRED =
            Create(6000, "ERR_MFA_REQUIRED",
                "Multi-factor authentication is required.",
                "MFA", "Warning");

        public static readonly ErrorCodeType MFA_INVALID_CODE =
            Create(6001, "ERR_MFA_INVALID_CODE",
                "MFA code is invalid.",
                "MFA", "Error");

        public static readonly ErrorCodeType MFA_EXPIRED =
            Create(6002, "ERR_MFA_EXPIRED",
                "MFA code has expired.",
                "MFA", "Warning");

        public static readonly ErrorCodeType MFA_LOCKED =
            Create(6003, "ERR_MFA_LOCKED",
                "MFA attempt is locked due to too many failures.",
                "MFA", "Error");

        // ========================================================================
        // 7000 API GATEWAY / RATE LIMIT
        // ========================================================================
        public static readonly ErrorCodeType RATE_LIMIT_EXCEEDED =
            Create(7000, "ERR_RATE_LIMIT_EXCEEDED",
                "Rate limit exceeded.",
                "RateLimit", "Warning");

        public static readonly ErrorCodeType TOO_MANY_REQUESTS =
            Create(7001, "ERR_TOO_MANY_REQUESTS",
                "Too many requests were sent.",
                "RateLimit", "Warning");

        public static readonly ErrorCodeType GATEWAY_TIMEOUT =
            Create(7002, "ERR_GATEWAY_TIMEOUT",
                "Upstream service timed out.",
                "APIGateway", "Error");

        public static readonly ErrorCodeType GATEWAY_BAD_RESPONSE =
            Create(7003, "ERR_GATEWAY_BAD_RESPONSE",
                "Upstream service sent an invalid response.",
                "APIGateway", "Error");

        // ========================================================================
        // 8000 BANKING
        // ========================================================================
        public static readonly ErrorCodeType ACCOUNT_NOT_FOUND =
            Create(8000, "ERR_ACCOUNT_NOT_FOUND",
                "Account does not exist.",
                "Banking", "Error");

        public static readonly ErrorCodeType INSUFFICIENT_BALANCE =
            Create(8001, "ERR_INSUFFICIENT_BALANCE",
                "Account balance is insufficient.",
                "Banking", "Warning");

        public static readonly ErrorCodeType TRANSACTION_NOT_ALLOWED =
            Create(8002, "ERR_TRANSACTION_NOT_ALLOWED",
                "This transaction type is not allowed.",
                "Banking", "Error");

        public static readonly ErrorCodeType BENEFICIARY_NOT_FOUND =
            Create(8003, "ERR_BENEFICIARY_NOT_FOUND",
                "Beneficiary does not exist.",
                "Banking", "Error");

        public static readonly ErrorCodeType TRANSFER_LIMIT_EXCEEDED =
            Create(8004, "ERR_TRANSFER_LIMIT_EXCEEDED",
                "Transfer amount exceeds limit.",
                "Banking", "Warning");

        public static readonly ErrorCodeType KYC_NOT_COMPLETED =
            Create(8005, "ERR_KYC_NOT_COMPLETED",
                "KYC verification not completed.",
                "Banking", "Error");

        public static readonly ErrorCodeType DUPLICATE_TRANSACTION =
            Create(8006, "ERR_DUPLICATE_TRANSACTION",
                "Duplicate transaction detected.",
                "Banking", "Error");

        public static readonly ErrorCodeType CURRENCY_NOT_SUPPORTED =
            Create(8007, "ERR_CURRENCY_NOT_SUPPORTED",
                "Currency is not supported.",
                "Banking", "Error");

        // ========================================================================
        // 9000 GENERIC DOMAIN
        // ========================================================================
        public static readonly ErrorCodeType DOMAIN_RULE_VIOLATED =
            Create(9000, "ERR_DOMAIN_RULE_VIOLATED",
                "A business rule has been violated.",
                "Domain", "Error");

        public static readonly ErrorCodeType BUSINESS_VALIDATION_FAILED =
            Create(9001, "ERR_BUSINESS_VALIDATION_FAILED",
                "Business validation failed.",
                "Domain", "Error");

        public static readonly ErrorCodeType OPERATION_CONFLICT =
            Create(9002, "ERR_OPERATION_CONFLICT",
                "Operation conflicts with the current state.",
                "Domain", "Error");

        public static readonly ErrorCodeType NOT_FOUND =
            Create(9003, "ERR_NOT_FOUND",
                "The requested resource was not found.",
                "NotFound", "Error");
    }
}
