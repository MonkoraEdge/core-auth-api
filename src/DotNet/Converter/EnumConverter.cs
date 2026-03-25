using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces;
using System.ComponentModel;
using System.Reflection;

namespace MonkoraEdge.Core.DotNet.Converter
{
    public static class EnumConverter
    {
        /// <summary>
        /// Convert enum to string (enum name)
        /// </summary>
        public static string ToName<T>(T enumValue) where T : Enum
            => enumValue.ToString();

        /// <summary>
        /// Convert enum to integer value
        /// </summary>
        public static int ToValue<T>(T enumValue) where T : Enum
            => Convert.ToInt32(enumValue);

        /// <summary>
        /// Convert string → enum (safe)
        /// </summary>
        public static T? FromName<T>(string name) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return Enum.TryParse(name, true, out T result) ? result : null;
        }

        /// <summary>
        /// Convert int → enum (safe)
        /// </summary>
        public static T? FromValue<T>(int value) where T : struct, Enum
        {
            if (Enum.IsDefined(typeof(T), value))
                return (T)Enum.ToObject(typeof(T), value);

            return null;
        }

        /// <summary>
        /// Enum → DescriptionAttribute
        /// </summary>
        public static string ToDescription<T>(T enumValue) where T : Enum
        {
            var field = typeof(T).GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description;
        }

        public static string ToDescription(Enum enumValue)
        {
            var enumType = enumValue.GetType();
            var field = enumType.GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description;
        }

        /// <summary>
        /// Enum → Display(Name) Attribute
        /// </summary>
        public static string ToDisplayName<T>(T enumValue) where T : Enum
        {
            var field = typeof(T).GetField(enumValue.ToString());
            var attr = field?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
            return attr?.Name;
        }

        public static string ToDisplayName(Enum enumValue)
        {
            var enumType = enumValue.GetType();
            var field = enumType.GetField(enumValue.ToString());
            var attr = field?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
            return attr?.Name;
        }

        /// <summary>
        /// Enum → Localized Text (ใช้ Localization Service)
        /// ตัวอย่าง: TransactionType.TRANSFER → “โอนเงิน”
        /// </summary>
        public static string ToLocalizedName<T>(
            T enumValue,
            ILocalization localizer
        ) where T : Enum
        {
            var key = $"{typeof(T).Name}.{enumValue}";
            return localizer.GetText(key); // resources: TransactionType.TRANSFER  
        }

        public static string ToLocalizedName(
            Enum enumValue,
            ILocalization localizer)
        {
            var key = $"{enumValue.GetType().Name}.{enumValue}";
            return localizer.GetText(key);
        }

        /// <summary>
        /// ใช้ตรวจสอบว่า enum มีค่าที่ระบุหรือไม่
        /// </summary>
        public static bool IsValid<T>(string name) where T : struct, Enum
            => Enum.TryParse(name, true, out T _);

        public static bool IsValid<T>(int value) where T : struct, Enum
            => Enum.IsDefined(typeof(T), value);


        
        /// <summary>
        /// แปลง enum เป็น key-value object (ใช้ใน API responses)
        /// </summary>
        public static object ToObject<T>(T enumValue) where T : Enum
            => new
            {
                Name = enumValue.ToString(),
                Value = Convert.ToInt32(enumValue),
                Description = ToDescription(enumValue),
                Display = ToDisplayName(enumValue),
            };

        public static object ToObject(Enum enumValue)
            => new
            {
                Name = enumValue.ToString(),
                Value = Convert.ToInt32(enumValue),
                Description = ToDescription(enumValue),
                Display = ToDisplayName(enumValue),
            };
    }
}
