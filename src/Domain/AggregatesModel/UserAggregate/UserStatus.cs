
namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate
{
    /// <summary>
    /// Allowed values for <see cref="User.Status"/>.
    /// Using named constants prevents magic-string drift across the codebase.
    /// </summary>
    public static class UserStatus
    {
        public const string Active = "ACTIVE";
        public const string Inactive = "INACTIVE";
        public const string Suspended = "SUSPENDED";
    }
}
