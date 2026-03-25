using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces;
using MonkoraEdge.Core.DotNet.Converter;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    //Domain/Application
    public static class EnumExtensions
    {
        public static string GetName<T>(this T enumTarget) where T : Enum => enumTarget.ToString();

        public static T GetEnum<T>(this string enumValue) where T : Enum => (T)Enum.Parse(typeof(T), enumValue, ignoreCase: true);

        /// <summary>
        /// Get enum name as string.
        /// Example: TransactionType.Deposit  "Deposit"
        /// </summary>
        public static string GetName(this Enum enumValue)
            => EnumConverter.ToName(enumValue);

        /// <summary>
        /// Get enum integer value.
        /// Example: TransactionType.Deposit  1
        /// </summary>
        public static int GetValue(this Enum enumValue)
            => EnumConverter.ToValue(enumValue);

        /// <summary>
        /// Get Description attribute text.
        /// Example:
        /// [Description("Deposit money")]
        /// Deposit  "Deposit money"
        /// </summary>
        public static string? GetDescription(this Enum enumValue)
            => EnumConverter.ToDescription(enumValue);

        /// <summary>
        /// Get Display(Name=) attribute text.
        /// Example:
        /// [Display(Name="Deposit")]
        /// Deposit  "Deposit"
        /// </summary>
        public static string? GetDisplayName(this Enum enumValue)
            => EnumConverter.ToDisplayName(enumValue);

        /// <summary>
        /// Get localized enum text using localization service.
        /// Must have: "EnumName.EnumValue" in resource JSON.
        /// Example: localizer.Localize(TransactionType.Transfer)
        /// </summary>
        public static string GetLocalizedText(
            this Enum enumValue,
            ILocalization localizer)
            => EnumConverter.ToLocalizedName(enumValue, localizer);

        /// <summary>
        /// Check if the name matches any enum item.
        /// Example: "Deposit".IsValidEnum&lt;TransactionType&gt;()  true
        /// </summary>
        public static bool IsValidEnum<T>(this string name) where T : struct, Enum
            => EnumConverter.IsValid<T>(name);

        /// <summary>
        /// Check if the integer matches any enum item.
        /// Example: 1.IsValidEnum&lt;TransactionType&gt;()  true
        /// </summary>
        public static bool IsValidEnum<T>(this int value) where T : struct, Enum
            => EnumConverter.IsValid<T>(value);

        /// <summary>
        /// Convert string to enum (safe)
        /// Example:
        /// "deposit".ToEnum&lt;TransactionType&gt;()  TransactionType.Deposit
        /// </summary>
        public static T? ToEnum<T>(this string name) where T : struct, Enum
            => EnumConverter.FromName<T>(name);

        /// <summary>
        /// Convert int to enum (safe)
        /// Example:
        /// 1.ToEnum&lt;TransactionType&gt;()  TransactionType.Deposit
        /// </summary>
        public static T? ToEnum<T>(this int value) where T : struct, Enum
            => EnumConverter.FromValue<T>(value);

        /// <summary>
        /// Convert enum  object for API responses.
        /// Provides:
        /// { name: "Deposit", value: 1, description: "...", display: "..." }
        /// </summary>
        public static object ToEnumObject(this Enum enumValue)
            => EnumConverter.ToObject(enumValue);
    }


    //public enum TransactionType
    //{
    //    [Description("Deposit money into account")]
    //    [Display(Name = "Deposit")]
    //    Deposit = 1,

    //    [Description("Withdraw money from account")]
    //    [Display(Name = "Withdraw")]
    //    Withdraw = 2,

    //    [Description("Transfer money to another account")]
    //    [Display(Name = "Transfer")]
    //    Transfer = 3,
    //}

    //var name = TransactionType.Deposit.GetName();
    //    // "Deposit"

}
