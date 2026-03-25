using FluentValidation.Results;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.FactoryAggregate
{
    public static class ExceptionFactory
    {
        public static CustomHttpException Create(
            string module,
            ErrorCodeType errorCode,
            string errorMessage = null,
            IList<ErrorField> fields = null,
            IList<ErrorRow> rows = null)
        {
            errorMessage ??= errorCode.Description;

            // Build exception with fields/rows when provided
            CustomHttpException BuildValidation() =>
                fields?.Count > 0 || rows?.Count > 0
                    ? new CustomHttpBadRequestException(module, fields ?? new List<ErrorField>(), rows ?? new List<ErrorRow>())
                    : new CustomHttpBadRequestException(module, errorCode, errorMessage);

            return errorCode.Category switch
            {
                // 2000 — Validation
                "Validation" => BuildValidation(),

                // 3000 — Authentication / Permission
                "Auth" => errorCode == ErrorCodeType.FORBIDDEN
                            ? new CustomHttpForbiddenException(module, errorCode, errorMessage)
                            : new CustomHttpUnauthorizedException(module, errorCode, errorMessage),
                "Permission" => new CustomHttpForbiddenException(module, errorCode, errorMessage),

                // Not Found
                "NotFound" => new CustomHttpNotFoundException(module, errorCode, errorMessage),

                // Not Supported
                "NotSupported" => new CustomNotSupportedException(module, errorCode, errorMessage),

                // Domain / Conflict
                "Domain" => new CustomHttpDomainRuleException(module, errorCode, errorMessage),

                // Rate Limit (RATE_LIMIT_EXCEEDED / TOO_MANY_REQUESTS have category "RateLimit")
                "RateLimit" => new CustomHttpTooManyRequestsException(module, errorCode, errorMessage),

                // API Key
                "APIKey" => new CustomHttpUnauthorizedException(module, errorCode, errorMessage),

                // OAuth2.1
                "OAuth2.1" => new CustomHttpUnauthorizedException(module, errorCode, errorMessage),

                // mTLS
                "mTLS" => new CustomHttpUnauthorizedException(module, errorCode, errorMessage),

                // MFA
                "MFA" => new CustomHttpUnauthorizedException(module, errorCode, errorMessage),

                // API Gateway (GATEWAY_TIMEOUT / GATEWAY_BAD_RESPONSE)
                "APIGateway" => new CustomHttpGatewayTimeoutException(module, errorCode, errorMessage),

                // Banking
                "Banking" => new CustomHttpDomainRuleException(module, errorCode, errorMessage),

                // System / Infrastructure
                "System" or "Infrastructure" => new CustomHttpInternalServerException(module, errorCode, errorMessage),

                // default
                _ => new CustomHttpInternalServerException(module, errorCode, errorMessage)
            };
        }

        // Shortcuts -------------------------------------------------------

        public static CustomHttpException BadRequest(string module, ErrorCodeType errorCode, string errorMessage = null)
            => new CustomHttpBadRequestException(module, errorCode, errorMessage ?? errorCode.Description);

        public static CustomHttpException Validation(string module, ErrorCodeType errorCode, string errorMessage = null)
            => new CustomHttpValidationException(module, errorCode, errorMessage ?? errorCode.Description);

        public static CustomHttpException Validation(string module, IList<ErrorField> fields)
            => new CustomHttpValidationException(module, fields);

        public static CustomHttpException Validation(string module, IList<ErrorRow> rows)
            => new CustomHttpValidationException(module, rows);

        public static CustomHttpException Validation(string module, IList<ErrorField> fields, IList<ErrorRow> rows)
            => new CustomHttpValidationException(module, fields, rows);

        public static CustomHttpException Validation(string module, IEnumerable<ValidationFailure> failures)
            => new CustomHttpValidationException(module, failures);

        public static CustomHttpException NotFound(string module, ErrorCodeType errorCode = null, string errorMessage = null)
        {
            errorCode ??= ErrorCodeType.NOT_FOUND;
            return new CustomHttpNotFoundException(module, errorCode, errorMessage ?? errorCode.Description);
        }

        public static CustomHttpException Unauthorized(string module, ErrorCodeType errorCode = null, string errorMessage = null)
        {
            errorCode ??= ErrorCodeType.UNAUTHORIZED;
            return new CustomHttpUnauthorizedException(module, errorCode, errorMessage ?? errorCode.Description);
        }

        public static CustomHttpException Forbidden(string module, ErrorCodeType errorCode = null, string errorMessage = null)
        {
            errorCode ??= ErrorCodeType.FORBIDDEN;
            return new CustomHttpForbiddenException(module, errorCode, errorMessage ?? errorCode.Description);
        }

        public static CustomHttpException Conflict(string module, ErrorCodeType errorCode, string errorMessage = null)
            => new CustomHttpConflictException(module, errorCode, errorMessage ?? errorCode.Description);

        public static CustomHttpException TooManyRequests(string module, ErrorCodeType errorCode = null, string errorMessage = null)
        {
            errorCode ??= ErrorCodeType.TOO_MANY_REQUESTS;
            return new CustomHttpTooManyRequestsException(module, errorCode, errorMessage ?? errorCode.Description);
        }

        public static CustomHttpException DomainRule(string module, ErrorCodeType errorCode, string errorMessage = null)
            => new CustomHttpDomainRuleException(module, errorCode, errorMessage ?? errorCode.Description);

        public static CustomHttpException Unexpected(string module, ErrorCodeType errorCode = null, string errorMessage = null)
        {
            errorCode ??= ErrorCodeType.UNEXPECTED;
            return new CustomHttpInternalServerException(module, errorCode, errorMessage ?? errorCode.Description);
        }

        public static CustomHttpException NotSupported(string module, ErrorCodeType errorCode = null, string errorMessage = null)
        {
            errorCode ??= ErrorCodeType.OPERATION_NOT_SUPPORTED;
            return new CustomNotSupportedException(module, errorCode, errorMessage ?? errorCode.Description);
        }
    }
}
