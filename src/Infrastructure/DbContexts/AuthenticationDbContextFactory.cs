using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts
{
    [ExcludeFromCodeCoverage]
    public sealed class AuthenticationContextDesignFactory : IDesignTimeDbContextFactory<AuthenticationDbContext>
    {
        public AuthenticationDbContext CreateDbContext(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";

            var apiProjectPath = ResolveApiProjectPath();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration["POSTGRES_CONNECTIONSTRING"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "POSTGRES_CONNECTIONSTRING was not found. Set it via appsettings or environment variables before running dotnet ef.");
            }

            var dbContextOptions = new DbContextOptionsBuilder<AuthenticationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new AuthenticationDbContext(dbContextOptions);
        }

        private static string ResolveApiProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var candidates = new[]
            {
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "API")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "API")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "API"))
        };

            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException(
                "Unable to locate the API project directory for appsettings resolution.");
        }
    }
}
