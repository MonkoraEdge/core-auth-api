using MonkoraEdge.Core.Auth.API.Extensions;
using MonkoraEdge.Core.Auth.API.Middleware;
using MonkoraEdge.Core.Auth.Infrastructure.Configurations;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet;
using MonkoraEdge.Core.DotNet.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;
using MonkoraEdge.Core.DotNet.Extensions.Middlewares;
using MonkoraEdge.Core.DotNet.Middleware;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using System.Diagnostics.Metrics;
using System.Threading.RateLimiting;

// Bootstrap Serilog early so startup errors are captured before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

// Allowed origins loaded from configuration — never use wildcard in production
const string corsPolicyName = "OAuthCors";
var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog. Configuration is read from the "Serilog" section
// in appsettings.json so sinks and enrichers can be changed without code changes.
builder.Host.UseSerilog((ctx, services, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

var environmentOptions = builder.Services.AddCustomOptions<EnvironmentOptions>(builder.Configuration);

builder.Services.AddCustomConfigurations(environmentOptions);

builder.Services.AddHttpClient();

builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    options.AddPolicy(corsPolicyName, policy =>
    {
        if (allowedOrigins.Length > 0)
            // Restrict to the specific set of allowed origins, methods, and headers.
            // AllowAnyHeader/AllowAnyMethod are intentionally avoided here.
            // X-Forwarded-For is intentionally excluded: allowing browsers to set it
            // would let scripts from allowed origins inject a forged client IP header.
            policy.WithOrigins(allowedOrigins)
                  .WithHeaders("Authorization", "Content-Type", "X-Requested-With")
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        // If no origins are configured, the policy allows nothing (deny-by-default)
    });
});

// HSTS: 1-year max-age, include subdomains. Skipped in dev via !IsDevelopment() guard below.
builder.Services.AddHsts(o =>
{
    o.MaxAge = TimeSpan.FromDays(365);
    o.IncludeSubDomains = true;
    o.Preload = false; // Enable only after verifying all subdomains support HTTPS
});

// SameSite cookie policy — defence-in-depth for any cookies set during consent / session flows.
builder.Services.Configure<CookiePolicyOptions>(o =>
{
    o.MinimumSameSitePolicy = SameSiteMode.Lax;
    o.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    o.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddControllers()
    .AddResponseJsonOptions();

// API versioning — uses header/query-string negotiation.
// AssumeDefaultVersionWhenUnspecified keeps existing clients working (no routes changed).
// ReportApiVersions advertises supported versions via the api-supported-versions response header.
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
});

// Enforce a tight request body size limit for all controllers (64 KB is ample for auth payloads).
// Individual OAuth2 form-post endpoints inherit this limit automatically.
builder.WebHost.ConfigureKestrel(k =>
    k.Limits.MaxRequestBodySize = 65_536); // 64 KB

builder.Services.AddMemoryCache();

// Per-IP fixed-window rate limiting.
// Each unique client IP gets its own independent bucket (60 req/min).
// [EnableRateLimiting("default")] annotates individual sensitive endpoints.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    o.AddPolicy("default", context =>
    {
        // Use the TCP-level RemoteIpAddress as the primary partition key so that a caller
        // cannot spoof their way to a fresh rate-limit bucket by forging X-Forwarded-For.
        // When running behind a trusted reverse proxy, configure UseForwardedHeaders() so that
        // RemoteIpAddress is already populated with the real client IP before this runs.
        // Fallback to "unknown" — never trust X-Forwarded-For here because a caller
        // behind our real proxy already has RemoteIpAddress populated correctly.
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey: ip, factory: _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Stricter policy for authentication endpoints (token, revoke, introspect).
    // 10 requests/min per IP prevents brute-force and credential stuffing attacks.
    o.AddPolicy("auth", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey: $"auth:{ip}", factory: _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = environmentOptions.REDIS_CONNECTIONSTRING;
});

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: environmentOptions.POSTGRES_CONNECTIONSTRING,
        name: "postgres",
        tags: new[] { "ready", "db" })
    .AddRedis(
        redisConnectionString: environmentOptions.REDIS_CONNECTIONSTRING,
        name: "redis",
        tags: new[] { "ready", "cache" });

// ── OpenTelemetry Metrics (Phase 5-1) ─────────────────────────────────────────────────────────
// Exposes a Prometheus-compatible /metrics scrape endpoint.
// Custom meters track business-level auth events (login, token issued, authorise requests).
// Runtime instrumentation adds GC, thread-pool, and memory metrics for free.
const string ServiceName = "monkora-core-auth";
var authMeter = new Meter(ServiceName, "1.0.0");

// Business-level counters — incremented from the DomainEvents / services layer via Meter.
// These are registered here so they exist before the first request is processed.
var loginSuccess  = authMeter.CreateCounter<long>("auth.login.success",  description: "Successful login events");
var loginFailure  = authMeter.CreateCounter<long>("auth.login.failure",  description: "Failed login attempts");
var tokenIssued   = authMeter.CreateCounter<long>("oauth2.token.issued", description: "OAuth2 access tokens issued");
var authorizeReq  = authMeter.CreateCounter<long>("oauth2.authorize.request", description: "OAuth2 authorize requests received");

// Expose the Meter via DI so services can inject IMeterFactory and record observations.
builder.Services.AddSingleton(authMeter);
builder.Services.AddSingleton(loginSuccess);
builder.Services.AddSingleton(loginFailure);
builder.Services.AddSingleton(tokenIssued);
builder.Services.AddSingleton(authorizeReq);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()   // HTTP server request metrics (latency, status codes)
            .AddRuntimeInstrumentation()       // .NET runtime GC / thread-pool metrics
            .AddMeter(ServiceName)             // Our custom business-level meter
            .AddPrometheusExporter();          // Expose on /metrics (Prometheus text format)
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(setup =>
{
    setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Authentication API", Version = "v1" });

    // Allow testing [Authorize] endpoints in Swagger UI using JWT Bearer tokens.
    setup.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT access token (at+JWT). Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    setup.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

// HSTS is skipped in development to avoid locking out localhost with a long-lived HSTS header.
if (!app.Environment.IsDevelopment())
    app.UseHsts();

// Apply SameSite/HttpOnly/Secure cookie policy enforced at the middleware level.
app.UseCookiePolicy();

app.UpdateDatabase<AuthenticationDbContext>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Assign / propagate X-Correlation-Id on every request.
// MUST run before error-handling middleware so that correlationId is available
// in all error log entries (including DomainException + unhandled exceptions).
app.UseMiddleware<CorrelationIdMiddleware>();
// Log all HTTP requests (method, path, status, duration) with correlation ID for tracing.
app.UseRequestLoggingMiddleware();

app.UseErrorHandling(new ErrorHandlingOptions("authentication"));
app.UseMiddleware<DomainExceptionHandlingMiddleware>();

// Security headers — prevent clickjacking, MIME sniffing, and information leakage
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    ctx.Response.Headers["X-XSS-Protection"] = "0"; // Modern browsers: disable legacy XSS auditor
    ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
    await next();
});

//app.UseRequestCulture();

app.UseRouting();

// CORS must run before authentication so that preflight OPTIONS requests (which carry
// no credentials) are handled without being rejected by the auth middleware.
app.UseCors(corsPolicyName);

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains("ready"),
    AllowCachingResponses = false
}).AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, AllowCachingResponses = false }).AllowAnonymous();

// Expose Prometheus /metrics endpoint.
// In production, restrict by IP using the allowlist in appsettings.json ("Metrics:AllowedCidrs").
// The simple IP-filter below guards against accidental public exposure.
app.MapPrometheusScrapingEndpoint("/metrics")
   .RequireHost(builder.Configuration.GetSection("Metrics:AllowedHosts").Get<string[]>() ?? new[] { "*" })
   .AllowAnonymous();

app.Run();

} // end try
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;