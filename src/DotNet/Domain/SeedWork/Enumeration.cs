
namespace MonkoraEdge.Core.DotNet.Domain.SeedWork
{
    public abstract class Enumeration : IComparable
    {
        public string Name { get; }
        public int Value { get; }

        protected Enumeration(int value, string name)
        {
            Value = value;
            Name = name;
        }

        public override string ToString() => Name;

        public override bool Equals(object? obj)
        {
            if (obj is not Enumeration otherEnum)
                return false;

            return Value == otherEnum.Value &&
                   Name == otherEnum.Name;
        }

        public override int GetHashCode() =>
            HashCode.Combine(Value, Name);

        public int CompareTo(object? other)
        {
            if (other is null) return 1;
            return Value.CompareTo(((Enumeration)other).Value);
        }

        public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
            typeof(T)
                .GetFields(System.Reflection.BindingFlags.Public |
                           System.Reflection.BindingFlags.Static |
                           System.Reflection.BindingFlags.DeclaredOnly)
                .Select(f => f.GetValue(null))
                .Cast<T>();

        public static T FromValue<T>(int value) where T : Enumeration =>
            GetAll<T>().First(e => e.Value == value);

        public static T FromName<T>(string name) where T : Enumeration =>
            GetAll<T>().First(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}


// public sealed class TransactionType : Enumeration
// {
//     public static readonly TransactionType Deposit  = new(1, "DEPOSIT");
//     public static readonly TransactionType Withdraw = new(2, "WITHDRAW");
//     public static readonly TransactionType Transfer = new(3, "TRANSFER");
//     private TransactionType(int value, string name) : base(value, name) { }
// }
// var type  = TransactionType.FromValue<TransactionType>(1);
// var type2 = TransactionType.FromName<TransactionType>("TRANSFER");
