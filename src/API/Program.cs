using MonkoraEdge.Core.Auth.API.Extensions;
using MonkoraEdge.Core.Auth.API.Middleware;
using MonkoraEdge.Core.Auth.Infrastructure.Configurations;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet;
using MonkoraEdge.Core.DotNet.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;
using MonkoraEdge.Core.DotNet.Extensions.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

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
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        // If no origins are configured, the policy allows nothing (deny-by-default)
    });
});

builder.Services.AddControllers()
    .AddResponseJsonOptions();

builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerAuthenticationOptions>, JwtBearerAuthenticationPostConfigureOptions>();
builder.Services.AddSingleton<IPostConfigureOptions<BasicAuthenticationOptions>, BasicAuthenticationPostConfigureOptions>();
builder.Services.AddSingleton<IPostConfigureOptions<ApiKeyAuthenticationOptions>, ApiKeyAuthenticationPostConfigureOptions>();

builder.Services.AddMemoryCache();

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

app.UseHsts();

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
    await next();
});

//app.UseRequestCulture();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseCors(corsPolicyName);

app.MapControllers();
app.MapHealthChecks("/health/ready", new HealthCheckOptions()).AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();

app.Run();