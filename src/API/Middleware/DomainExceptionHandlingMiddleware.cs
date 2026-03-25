using System.Text.Json;
using System.Text.Json.Serialization;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.Auth.API.Middleware;

public class DomainExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public DomainExceptionHandlingMiddleware(RequestDelegate next, ILogger<DomainExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            var response = new ErrorMessageResponse
            {
                Code = ex.ErrorCode.Name,
                Message = ex.ErrorMessage,
                Module = ex.Module,
                Category = ex.ErrorCode.Category,
                Severity = ex.ErrorCode.Severity,
                Fields = ex.Fields,
                Rows = ex.Rows,
                CorrelationId = ex.CorrelationId ?? Guid.NewGuid().ToString("N"),
                OccurredAt = DateTime.UtcNow
            };

            context.Response.StatusCode = MapStatusCode(ex.ErrorCode);
            context.Response.ContentType = "application/json";

            _logger.LogError(
                "Handled Domain Exception: {ErrorCode} | {Message} | Module: {Module} | Trace: {TraceId}",
                ex.ErrorCode.Name,
                ex.ErrorMessage,
                ex.Module,
                response.CorrelationId);

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }

    private static int MapStatusCode(ErrorCodeType code) =>
        code.Category switch
        {
            "Validation" => StatusCodes.Status400BadRequest,
            "Auth" => StatusCodes.Status401Unauthorized,
            "Permission" => StatusCodes.Status403Forbidden,
            "NotFound" => StatusCodes.Status404NotFound,
            "Conflict" or "Domain" or "Banking" => StatusCodes.Status409Conflict,
            "RateLimit" => StatusCodes.Status429TooManyRequests,
            "APIGateway" => StatusCodes.Status502BadGateway,
            "NotSupported" => StatusCodes.Status501NotImplemented,
            "APIKey" or "OAuth2.1" or "mTLS" or "MFA" => StatusCodes.Status401Unauthorized,
            "System" or "Infrastructure" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
}