using MonkoraEdge.Core.DotNet.AggregatesModel.ResourceAggregate;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AddressAggregate
{
    public class AddressCountryValue
    {
        public ResourceValue Country { get; set; }
        public ResourceValue Province { get; set; }
        public ResourceValue District { get; set; }
        public ResourceValue SubDistrict { get; set; }
        public ResourceValue PostalCode { get; set; }

        public AddressCountryValue()
        {

        }
    }
}