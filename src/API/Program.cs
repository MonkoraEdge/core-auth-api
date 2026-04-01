using MonkoraEdge.Core.Auth.API.Extensions;
using MonkoraEdge.Core.Auth.API.Middleware;
using MonkoraEdge.Core.Auth.Infrastructure.Configurations;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet;
using MonkoraEdge.Core.DotNet.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;
using MonkoraEdge.Core.DotNet.Extensions.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

// Allowed origins loaded from configuration — never use wildcard in production
const string corsPolicyName = "OAuthCors";
var builder = WebApplication.CreateBuilder(args);
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
            policy.WithOrigins(allowedOrigins)
                  .WithHeaders("Authorization", "Content-Type", "X-Requested-With", "X-Forwarded-For")
                  .WithMethods("GET", "POST", "PUT", "DELETE")
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
        var ip = context.Connection.RemoteIpAddress?.ToString()
                 ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                 ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey: ip, factory: _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
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

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(setup =>
{
    setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Authentication API", Version = "v1" });
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
app.MapHealthChecks("/health/ready", new HealthCheckOptions()).AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();

app.Run();