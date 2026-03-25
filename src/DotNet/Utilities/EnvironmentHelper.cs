namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class EnvironmentHelper
    {
        public static class VariableTypes
        {
            public const string ASPNETCORE_ENVIRONMENT = "ASPNETCORE_ENVIRONMENT";
        }

        public static class System
        {
            public static readonly string ASPNETCORE_ENVIRONMENT = Environment.GetEnvironmentVariable(VariableTypes.ASPNETCORE_ENVIRONMENT);
        }

        public static string Get(string key, string defaultValue = "")
            => Environment.GetEnvironmentVariable(key) ?? defaultValue;

        public static bool IsLocal => System.ASPNETCORE_ENVIRONMENT?.ToLower() == "local";

        public static bool IsDevelopment => System.ASPNETCORE_ENVIRONMENT?.ToLower() == "development";

        public static bool IsUAT => System.ASPNETCORE_ENVIRONMENT?.ToLower() == "uat";

        public static bool IsProduction => System.ASPNETCORE_ENVIRONMENT?.ToLower() == "production";

        public static bool IsUnitTest => System.ASPNETCORE_ENVIRONMENT?.ToLower() == "unittest";

        public static bool IsIntegrationTest => System.ASPNETCORE_ENVIRONMENT?.ToLower() == "integrationtest";

        public static string GetCurrentEnvironmentName()
        {
            if (IsLocal)
                return "Local";

            if (IsDevelopment)
                return "Development";

            if (IsUAT)
                return "UAT";

            if (IsProduction)
                return "Production";

            if (IsUnitTest)
                return "UnitTest";

            if (IsIntegrationTest)
                return "IntegrationTest";

            return "N/A";
        }

        public static string GetCurrentEnvironmentShortenName()
        {
            if (IsLocal)
                return "Local";

            if (IsDevelopment)
                return "Dev";

            if (IsUAT)
                return "UAT";

            if (IsProduction)
                return "Prod";

            if (IsUnitTest)
                return "UnitTest";

            if (IsIntegrationTest)
                return "IntTest";

            return "N/A";
        }

        private static string GenerateCacheKeyPrefix()
        {
            if (IsProduction)
                return string.Empty;

            return $"{GetCurrentEnvironmentShortenName()}_".ToLower();
        }

        public static string GenerateCacheKey(string apiName, string moduleName, string keyName)
        {
            var env = GenerateCacheKeyPrefix();

            return $"{env}{apiName}_{moduleName}_{keyName}".ToLower();
        }

        public static int GetMaxDegreeOfParallelism()
        {
            var maxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 0.4));

            if (maxDegreeOfParallelism < 1)
                maxDegreeOfParallelism = 1;

            return maxDegreeOfParallelism;
        }

    }
}
