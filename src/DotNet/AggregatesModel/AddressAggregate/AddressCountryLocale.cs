namespace MonkoraEdge.Core.DotNet.AggregatesModel.AddressAggregate
{
    public class AddressCountryLocale
    {
        public AddressDataValueLocale Country { get; set; }
        public AddressDataValueLocale Province { get; set; }
        public AddressDataValueLocale District { get; set; }
        public AddressDataValueLocale SubDistrict { get; set; }
        public AddressDataValueLocale PostalCode { get; set; }
    }
}