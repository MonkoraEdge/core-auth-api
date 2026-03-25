using Microsoft.Extensions.Configuration;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class ConfigurationHelper
    {
        public static IConfigurationBuilder CreateConfigurationBuilder(string settingProjectDirectory, string settingJsonFile = "appsettings.json")
        {
            return new ConfigurationBuilder().SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", settingProjectDirectory)).AddJsonFile(settingJsonFile, false, true);
        }
    }
}
