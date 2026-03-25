namespace MonkoraEdge.Core.DotNet.Domain.ValueObjects
{
    public static class Guard
    {
        public static void AgainstNull(object input, string name)
        {
            if (input is null)
                throw new ArgumentNullException(name, $"{name} cannot be null.");
        }

        public static void AgainstNullOrEmpty(string input, string name)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException($"{name} cannot be empty.", name);
        }

        public static void AgainstOutOfRange(decimal value, decimal min, decimal max, string name)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(name, $"{name} must be between {min} and {max}.");
        }

        /// <summary>Throws when value equals default(T) (e.g. Guid.Empty, 0, null).</summary>
        public static void AgainstDefault<T>(T value, string name) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
                throw new ArgumentException($"{name} cannot be the default value.", name);
        }

        /// <summary>Throws when value is less than zero.</summary>
        public static void AgainstNegative(decimal value, string name)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
        }

        /// <summary>Throws when value is less than or equal to zero.</summary>
        public static void AgainstZeroOrNegative(decimal value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name, $"{name} must be greater than zero.");
        }

        /// <summary>Throws when the Guid is empty (Guid.Empty).</summary>
        public static void AgainstInvalidGuid(Guid id, string name)
        {
            if (id == Guid.Empty)
                throw new ArgumentException($"{name} cannot be an empty Guid.", name);
        }

        /// <summary>Throws when the collection is null or contains no elements.</summary>
        public static void AgainstNullOrEmpty<T>(IEnumerable<T> collection, string name)
        {
            if (collection is null || !collection.Any())
                throw new ArgumentException($"{name} cannot be null or empty.", name);
        }

        /// <summary>Throws when value exceeds the maximum allowed length.</summary>
        public static void AgainstMaxLength(string input, int maxLength, string name)
        {
            if (input != null && input.Length > maxLength)
                throw new ArgumentException($"{name} cannot exceed {maxLength} characters.", name);
        }
    }
}
