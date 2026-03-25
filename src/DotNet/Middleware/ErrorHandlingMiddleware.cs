using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using MonkoraEdge.Core.DotNet.AggregatesModel.BuilderAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces;

namespace MonkoraEdge.Core.DotNet.Middleware
{
    public class ErrorHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly ILocalization _localizer;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        public ErrorHandlingMiddleware(
            ILogger<ErrorHandlingMiddleware> logger,
            ILocalization localizer = null)
        {
            _logger = logger;
            _localizer = localizer;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);

                // Handle 404 from pipeline
                if (context.Response.StatusCode == StatusCodes.Status404NotFound &&
                    !context.Response.HasStarted)
                {
                    await WriteNotFoundResponse(context);
                }
            }
            catch (CustomHttpException ex)
            {
                await WriteCustomException(context, ex);
            }
            catch (Exception ex)
            {
                await WriteUnexpectedException(context, ex);
            }
        }

        // ================================================================
        // Handle Custom Exception
        // ================================================================
        private async Task WriteCustomException(HttpContext ctx, CustomHttpException ex)
        {
            var response = ErrorResponseBuilder.Build(ex, _localizer);
            var statusCode = MapStatusCode(ex.ErrorCode);

            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json";

            LogException(ex, response);

            var json = JsonSerializer.Serialize(response, JsonOptions);
            await ctx.Response.WriteAsync(json);
        }

        // ================================================================
        // Handle Unexpected Exception (SystemError / Unexpected)
        // ================================================================
        private async Task WriteUnexpectedException(HttpContext ctx, Exception ex)
        {
            var error = ErrorResponseBuilder.BuildUnexpected(ex, _localizer);

            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/json";

            LogUnexpected(ex, error);

            var json = JsonSerializer.Serialize(error, JsonOptions);
            await ctx.Response.WriteAsync(json);
        }

        // ================================================================
        // Handle No Endpoint / 404 From Routing
        // ================================================================
        private async Task WriteNotFoundResponse(HttpContext ctx)
        {
            var response = new ErrorMessageResponse
            {
                Code = ErrorCodeType.NOT_FOUND.Name,
                Message = _localizer?.GetText(ErrorCodeType.NOT_FOUND.Name) ?? "Resource not found.",
                Module = "system",
                Category = ErrorCodeType.NOT_FOUND.Category,
                Severity = ErrorCodeType.NOT_FOUND.Severity,
                Fields = Array.Empty<ErrorField>(),
                Rows = Array.Empty<ErrorRow>(),
                CorrelationId = Guid.NewGuid().ToString("N"),
                OccurredAt = DateTime.UtcNow
            };

            ctx.Response.ContentType = "application/json";

            _logger.LogWarning("404 Not Found at {Path}, Trace: {TraceId}",
                ctx.Request.Path,
                response.CorrelationId);

            await ctx.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }

        // ================================================================
        // Map ErrorCode  HTTP Status Code
        // ================================================================
        private int MapStatusCode(ErrorCodeType code) =>
            code.Category switch
            {
                "Validation"                            => StatusCodes.Status400BadRequest,
                "Auth"                                  => StatusCodes.Status401Unauthorized,
                "Permission"                            => StatusCodes.Status403Forbidden,
                "NotFound"                              => StatusCodes.Status404NotFound,
                "Conflict" or "Domain" or "Banking"     => StatusCodes.Status409Conflict,
                "RateLimit"                             => StatusCodes.Status429TooManyRequests,
                "APIGateway"                            => StatusCodes.Status502BadGateway,
                "NotSupported"                          => StatusCodes.Status501NotImplemented,
                "APIKey" or "OAuth2.1" or "mTLS" or "MFA" => StatusCodes.Status401Unauthorized,
                "System" or "Infrastructure"            => StatusCodes.Status500InternalServerError,
                _                                       => StatusCodes.Status500InternalServerError
            };

        // ================================================================
        // Logging (Serilog + OTel)
        // ================================================================
        private void LogException(CustomHttpException ex, ErrorMessageResponse response)
        {
            _logger.LogError(
                "Handled Exception: {ErrorCode} | {Message} | Module: {Module} | Severity: {Severity} | Trace: {TraceId}",
                ex.ErrorCode.Name,
                ex.ErrorMessage,
                ex.Module,
                ex.ErrorCode.Severity,
                response.CorrelationId
            );
        }

        private void LogUnexpected(Exception ex, ErrorMessageResponse response)
        {
            _logger.LogCritical(ex,
                "Unexpected Exception: {Message} | Trace: {TraceId}",
                ex.Message,
                response.CorrelationId
            );
        }
    }
}


//builder.Services.AddSingleton<ErrorHandlingMiddleware>();

//var app = builder.Build();

//app.UseMiddleware<ErrorHandlingMiddleware>();
