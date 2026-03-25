using System.Globalization;
using System.Text.Json;


namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class LocalizationHelper
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _resources = new();

        public static void LoadJsonFolder(string folderPath)
        {
            foreach (var file in Directory.GetFiles(folderPath, "*.json"))
            {
                var locale = Path.GetFileNameWithoutExtension(file).ToLower();
                var json = File.ReadAllText(file);

                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;

                _resources[locale] = dict;
            }
        }

        public static string Text(string key, string culture = null)
        {
            culture ??= CultureInfo.CurrentUICulture.Name.ToLower();

            if (_resources.TryGetValue(culture, out var dict) &&
                dict.TryGetValue(key, out var value))
            {
                return value;
            }

            // fallback → English
            if (_resources.TryGetValue("en", out var enDict) &&
                enDict.TryGetValue(key, out var enValue))
            {
                return enValue;
            }

            return key; // fallback
        }
    }
}
