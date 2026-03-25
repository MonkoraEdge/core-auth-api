using MonkoraEdge.Core.DotNet.AggregatesModel.ResourceAggregate;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AddressAggregate
{
    public class AddressCountryCode
    {
        public ResourceCode Country { get; set; }
        public ResourceCode Province { get; set; }
        public ResourceCode District { get; set; }
        public ResourceCode SubDistrict { get; set; }
        public ResourceCode PostalCode { get; set; }
    }
}