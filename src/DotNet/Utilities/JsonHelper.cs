using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class JsonHelper
    {
        /// <summary>
        /// Default JSON options for APIs (camelCase + ignore default + enum as string).
        /// </summary>
        public static readonly JsonSerializerOptions Options = CreateDefaultOptions();

        /// <summary>
        /// Default JSON options but with indentation (pretty print).
        /// Useful for logging or debugging.
        /// Pretty JSON (used with logging)
        /// </summary>
        public static readonly JsonSerializerOptions OptionsPretty = CreatePrettyOptions();

        // -------------------- FACTORY METHODS --------------------
        private static JsonSerializerOptions CreateDefaultOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                WriteIndented = false,
                Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
            };
        }

        private static JsonSerializerOptions CreatePrettyOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                WriteIndented = true,
                Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
            };
        }

        public static string Serialize(object obj)
            => JsonSerializer.Serialize(obj, Options);

        public static T? Deserialize<T>(string json)
            => JsonSerializer.Deserialize<T>(json, Options);

        public static bool TryDeserialize<T>(string json, out T? obj)
        {
            try
            {
                obj = Deserialize<T>(json);
                return obj is not null;
            }
            catch
            {
                obj = default;
                return false;
            }
        }
    }
}
