using AutoMapper;
using FluentValidation;
using MonkoraEdge.Core.DotNet.Infrastructure;
using MonkoraEdge.Core.DotNet.Infrastructure.Logging;
using MonkoraEdge.Core.DotNet.Middleware;
using MonkoraEdge.Core.DotNet.Security.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using MonkoraEdge.Core.DotNet.AggregatesModel.BehaviorAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache.Interfaces;
using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS;
using MonkoraEdge.Core.DotNet.Domain.Interfaces.Notifications;
using MonkoraEdge.Core.DotNet.Domain.Interfaces.DomainEvent;

namespace MonkoraEdge.Core.DotNet
{
    public static class DependencyInjection
    {
        // ================================================================
        // Database Migration
        // ================================================================

        public static void UpdateDatabase<T>(this IApplicationBuilder app) where T : DbContext
        {
            using var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();

            using var context = serviceScope.ServiceProvider.GetService<T>();
            context?.Database.Migrate();
        }

        // ================================================================
        // Options
        // ================================================================

        /// <summary>
        /// Bind a strongly-typed options class from configuration and return its instance.
        /// Throws InvalidOperationException if the section is missing or invalid.
        /// </summary>
        public static T AddCustomOptions<T>(this IServiceCollection services, IConfiguration configuration) where T : class
        {
            services.AddOptions<T>()
                .Bind(configuration)
                .ValidateDataAnnotations();

            var options = configuration.Get<T>()
                ?? throw new InvalidOperationException(
                    $"Configuration section for '{typeof(T).Name}' is missing or could not be bound.");

            return options;
        }

        // ================================================================
        // Validators (FluentValidation)
        // ================================================================

        /// <summary>
        /// Register all FluentValidation validators from the provided assembly (default: calling assembly).
        /// </summary>
        public static IServiceCollection AddAssemblyValidators(this IServiceCollection services, Assembly callingAssembly = null)
        {
            var assembly = callingAssembly ?? Assembly.GetCallingAssembly();
            services.AddValidatorsFromAssembly(assembly);
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            return services;
        }

        // ================================================================
        // Logging
        // ================================================================

        /// <summary>
        /// Register SerilogLogger as the ILoggerService&lt;T&gt; implementation.
        /// Configure Serilog externally via Log.Logger or builder.Host.UseSerilog().
        /// </summary>
        public static IServiceCollection AddLoggingServices(this IServiceCollection services)
        {
            services.AddSingleton(typeof(ILoggerService<>), typeof(SerilogLogger<>));
            return services;
        }

        // ================================================================
        // Current User
        // ================================================================

        /// <summary>
        /// Register ICurrentUserService backed by IHttpContextAccessor.
        /// Resolves the authenticated user from the JWT claims of the current HTTP request.
        /// </summary>
        public static IServiceCollection AddCurrentUserService(this IServiceCollection services)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            return services;
        }

        // ================================================================
        // CQRS Pipeline (Dispatcher + Behaviors)
        // ================================================================

        /// <summary>
        /// Register the CQRS pipeline: IDispatcher, ValidationBehavior, LoggingBehavior,
        /// PerformanceBehavior, plus all ICommandHandler and IQueryHandler implementations
        /// found in the provided assemblies.
        ///
        /// Usage in Program.cs:
        /// <code>
        /// builder.Services.AddCommandQueryPipeline(Assembly.GetExecutingAssembly());
        /// </code>
        /// </summary>
        public static IServiceCollection AddCommandQueryPipeline(
            this IServiceCollection services,
            params Assembly[] handlerAssemblies)
        {
            // Register Dispatcher
            services.AddScoped<IDispatcher, Dispatcher>();

            // Register pipeline behaviors as open generics (executed in this order)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

            // Scan assemblies for handler implementations
            foreach (var assembly in handlerAssemblies)
                RegisterHandlersFromAssembly(services, assembly);

            return services;
        }

        private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
        {
            var handlerInterfaces = new[]
            {
                typeof(ICommandHandler<>),
                typeof(ICommandHandler<,>),
                typeof(IQueryHandler<,>)
            };

            var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract);

            foreach (var type in types)
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (!iface.IsGenericType) continue;

                    var def = iface.GetGenericTypeDefinition();
                    if (Array.Exists(handlerInterfaces, h => h == def))
                        services.AddScoped(iface, type);
                }
            }
        }

        // ================================================================
        // Cache
        // ================================================================

        /// <summary>
        /// Register DistributedCacheProvider (IDistributedCache) as the default ICacheProvider.
        /// Call AddDistributedMemoryCache(), AddStackExchangeRedisCache(), or
        /// AddDistributedSqlServerCache() before this to configure the backing store.
        /// </summary>
        public static IServiceCollection AddProviderCacheServices(this IServiceCollection services)
        {
            services.AddSingleton<ICacheProvider, DistributedCacheProvider>();
            return services;
        }

        /// <summary>
        /// Register MemoryCacheProvider as ICacheProvider (in-process, no-serialization).
        /// Suitable for single-instance deployments or testing.
        /// </summary>
        public static IServiceCollection AddMemoryCacheServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
            return services;
        }

        // ================================================================
        // JWT Authentication
        // ================================================================

        /// <summary>
        /// Register JWT Bearer authentication using <see cref="JwtOptions"/> bound from
        /// <paramref name="configuration"/> section <c>"Jwt"</c> (or a custom section name).
        ///
        /// Required appsettings.json:
        /// <code>
        /// "Jwt": {
        ///   "Secret": "your-256-bit-secret",
        ///   "Issuer": "https://your-domain.com",
        ///   "Audience": "your-api"
        /// }
        /// </code>
        ///
        /// Pipeline (Program.cs):
        /// <code>
        /// builder.Services.AddJwtAuthentication(builder.Configuration);
        /// // ...
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// </code>
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = JwtOptions.SectionName)
        {
            var opts = new JwtOptions();
            configuration.GetSection(sectionName).Bind(opts);
            services.Configure<JwtOptions>(configuration.GetSection(sectionName));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(opts.Secret ?? string.Empty)),
                        ValidateIssuer = opts.ValidateIssuer,
                        ValidIssuer = opts.Issuer,
                        ValidateAudience = opts.ValidateAudience,
                        ValidAudience = opts.Audience,
                        ValidateLifetime = opts.ValidateLifetime,
                        ClockSkew = TimeSpan.FromSeconds(opts.ClockSkewSeconds)
                    };
                });

            services.AddAuthorization();
            return services;
        }

        // ================================================================
        // Domain Events
        // ================================================================

        /// <summary>
        /// Register <see cref="IDomainEventDispatcher"/> and scan assemblies for all
        /// <see cref="IDomainEventHandler{TEvent}"/> implementations.
        ///
        /// Domain events are automatically dispatched by <c>BaseDbContext</c> after
        /// each successful <c>SaveChangesAsync</c> when
        /// <see cref="IDomainEventDispatcher"/> is injected into your DbContext.
        ///
        /// Usage in Program.cs:
        /// <code>
        /// builder.Services.AddDomainEvents(Assembly.GetExecutingAssembly());
        /// </code>
        /// </summary>
        public static IServiceCollection AddDomainEvents(
            this IServiceCollection services,
            params Assembly[] handlerAssemblies)
        {
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            foreach (var assembly in handlerAssemblies)
            {
                var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract);
                foreach (var type in types)
                {
                    foreach (var iface in type.GetInterfaces())
                    {
                        if (!iface.IsGenericType) continue;
                        if (iface.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                            services.AddScoped(iface, type);
                    }
                }
            }

            return services;
        }

        // ================================================================
        // Notifications (fan-out pub/sub)
        // ================================================================

        /// <summary>
        /// Register <see cref="INotificationDispatcher"/> and scan assemblies for all
        /// <see cref="INotificationHandler{TNotification}"/> implementations.
        ///
        /// Usage in Program.cs:
        /// <code>
        /// builder.Services.AddNotifications(Assembly.GetExecutingAssembly());
        /// </code>
        /// </summary>
        public static IServiceCollection AddNotifications(
            this IServiceCollection services,
            params Assembly[] handlerAssemblies)
        {
            services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

            foreach (var assembly in handlerAssemblies)
            {
                var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract);
                foreach (var type in types)
                {
                    foreach (var iface in type.GetInterfaces())
                    {
                        if (!iface.IsGenericType) continue;
                        if (iface.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                            services.AddScoped(iface, type);
                    }
                }
            }

            return services;
        }

        // ================================================================
        // Health Checks
        // ================================================================

        /// <summary>
        /// Register default health checks and map the health endpoint at <c>/health</c>.
        /// Optionally add more checks via the returned <see cref="IHealthChecksBuilder"/>:
        /// <code>
        /// builder.Services
        ///     .AddDefaultHealthChecks()
        ///     .AddDbContextCheck&lt;AppDbContext&gt;()
        ///     .AddRedis("localhost:6379");
        /// </code>
        /// </summary>
        public static IHealthChecksBuilder AddDefaultHealthChecks(this IServiceCollection services)
        {
            return services.AddHealthChecks();
        }

        // ================================================================
        // CORS
        // ================================================================

        /// <summary>
        /// Register a named CORS policy that allows the specified origins.
        /// Call <c>app.UseCors(policyName)</c> in the middleware pipeline.
        ///
        /// Usage:
        /// <code>
        /// builder.Services.AddCorsPolicies("AllowFrontend",
        ///     "https://app.yourdomain.com", "https://admin.yourdomain.com");
        ///
        /// app.UseCors("AllowFrontend");
        /// </code>
        /// </summary>
        public static IServiceCollection AddCorsPolicies(
            this IServiceCollection services,
            string policyName,
            params string[] origins)
        {
            services.AddCors(o => o.AddPolicy(policyName, policy =>
            {
                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }));
            return services;
        }

        /// <summary>
        /// Register a permissive CORS policy that allows ANY origin (development / internal APIs only).
        /// Use <c>AddCorsPolicies</c> with explicit origins in production.
        /// </summary>
        public static IServiceCollection AddOpenCorsPolicy(
            this IServiceCollection services,
            string policyName = "AllowAll")
        {
            services.AddCors(o => o.AddPolicy(policyName, policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }));
            return services;
        }

        // ================================================================
        // Rate Limiting (.NET 9 built-in)
        // ================================================================

        /// <summary>
        /// Register a default fixed-window rate limiter on the "default" policy.
        /// Call <c>app.UseRateLimiter()</c> in the middleware pipeline.
        ///
        /// Defaults: 100 requests per 1-minute window, queue limit 10.
        ///
        /// Usage:
        /// <code>
        /// builder.Services.AddDefaultRateLimiting();
        /// // ...
        /// app.UseRateLimiter();
        /// </code>
        ///
        /// Annotate endpoints with <c>[EnableRateLimiting("default")]</c>.
        /// </summary>
        public static IServiceCollection AddDefaultRateLimiting(
            this IServiceCollection services,
            int permitLimit = 100,
            int windowMinutes = 1,
            int queueLimit = 10)
        {
            services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                o.AddFixedWindowLimiter("default", options =>
                {
                    options.PermitLimit = permitLimit;
                    options.Window = TimeSpan.FromMinutes(windowMinutes);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = queueLimit;
                });
            });
            return services;
        }

        // ================================================================
        // Localization
        // ================================================================

        /// <summary>
        /// Register the built-in <c>LocalizationService</c> (JSON file backed).
        /// Call <c>LocalizationHelper.LoadJsonFolder(path)</c> in Program.cs after registration
        /// to load language resource files.
        /// </summary>
        public static IServiceCollection AddLocalizationService(this IServiceCollection services)
        {
            services.TryAddSingleton<ILocalization, Localization>();
            return services;
        }

        // ================================================================
        // Error Handling Middleware (IMiddleware requires explicit service registration)
        // ================================================================

        /// <summary>
        /// Register <see cref="ErrorHandlingMiddleware"/> for use with
        /// <c>app.UseErrorHandlingMiddleware()</c>.
        ///
        /// Must be called before <c>app.UseErrorHandlingMiddleware()</c>.
        /// <code>
        /// builder.Services.AddErrorHandling();
        /// // ...
        /// app.UseErrorHandlingMiddleware();
        /// </code>
        /// </summary>
        public static IServiceCollection AddErrorHandling(this IServiceCollection services)
        {
            services.AddTransient<ErrorHandlingMiddleware>();
            return services;
        }

        // ================================================================
        // AutoMapper
        // ================================================================

        /// <summary>
        /// Register AutoMapper and scan the given assemblies for <c>Profile</c> subclasses.
        /// If no assemblies are provided, scans the calling assembly.
        ///
        /// <code>
        /// builder.Services.AddAutoMapperProfiles(Assembly.GetExecutingAssembly());
        /// </code>
        /// </summary>
        public static IServiceCollection AddAutoMapperProfiles(
            this IServiceCollection services,
            params Assembly[] profileAssemblies)
        {
            var assemblies = profileAssemblies.Length > 0
                ? profileAssemblies
                : new[] { Assembly.GetCallingAssembly() };

            services.AddAutoMapper(cfg =>
            {
                foreach (var asm in assemblies)
                    cfg.AddMaps(asm);
            });
            return services;
        }

        // ================================================================
        // Unit of Work
        // ================================================================

        /// <summary>
        /// Register <see cref="UnitOfWork"/> as <see cref="IUnitOfWork"/> (scoped).
        /// Requires a <typeparamref name="TDbContext"/> to be registered in DI.
        ///
        /// <code>
        /// builder.Services.AddUnitOfWork&lt;AppDbContext&gt;();
        /// </code>
        /// </summary>
        public static IServiceCollection AddUnitOfWork<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services.AddScoped<IUnitOfWork>(sp =>
                new UnitOfWork(sp.GetRequiredService<TDbContext>()));
            return services;
        }

        // ================================================================
        // Redis Cache (StackExchange.Redis provider)
        // ================================================================

        /// <summary>
        /// Register <see cref="RedisCacheProvider"/> as <see cref="ICacheProvider"/> (singleton).
        /// Requires a connection string to a running Redis instance.
        ///
        /// <code>
        /// builder.Services.AddRedisCacheServices("localhost:6379");
        /// </code>
        /// </summary>
        public static IServiceCollection AddRedisCacheServices(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(connectionString));
            services.AddSingleton<ICacheProvider, RedisCacheProvider>();
            return services;
        }
    }
}
