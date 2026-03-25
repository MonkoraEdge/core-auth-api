using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.Domain.ValueObjects
{
    public sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        private Money(decimal amount, string currency)
        {
            Guard.AgainstNullOrEmpty(currency, nameof(currency));
            Amount = amount;
            Currency = currency;
        }

        public static Money From(decimal amount, string currency = "THB")
            => new(amount, currency);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        public override string ToString() => $"{Amount:0.00} {Currency}";
    }
}
