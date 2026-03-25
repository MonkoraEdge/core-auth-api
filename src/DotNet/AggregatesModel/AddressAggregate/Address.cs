using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ResourceAggregate;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AddressAggregate
{
    public class Address : AddressCountryValue
    {
        private static readonly Lazy<AddressCountryValue?> _defaultLocation =
            new(LoadDefaultLocationFromCsv);

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Locale Description { get; set; } = new Locale();

        public Address()
        {
            var defaultLocation = _defaultLocation.Value;
            if (defaultLocation is not null)
            {
                SetLocationCountry(defaultLocation);
            }
        }

        public static IReadOnlyList<AddressCountryLocale> ReadThaiAddressLocales(string? csvPath = null)
            => AddressDataSourceReader.ReadThaiAddressLocales(csvPath);

        public static IReadOnlyList<AddressDataValueLocale> GetCountries(string? csvPath = null)
            => AddressDataSourceReader.GetCountries(csvPath);

        public static IReadOnlyList<AddressDataValueLocale> GetProvincesByCountry(string countryCode, string? csvPath = null)
            => AddressDataSourceReader.GetProvincesByCountry(countryCode, csvPath);

        public static IReadOnlyList<AddressDataValueLocale> GetDistrictsByProvince(string provinceCode, string? csvPath = null)
            => AddressDataSourceReader.GetDistrictsByProvince(provinceCode, csvPath);

        public static IReadOnlyList<AddressDataValueLocale> GetSubDistrictsByDistrict(string districtCode, string? csvPath = null)
            => AddressDataSourceReader.GetSubDistrictsByDistrict(districtCode, csvPath);

        public static IReadOnlyList<AddressDataValueLocale> GetPostalCodesBySubDistrict(string subDistrictCode, string? csvPath = null)
            => AddressDataSourceReader.GetPostalCodesBySubDistrict(subDistrictCode, csvPath);

        public bool TrySetLocationByCodes(
            string countryCode,
            string provinceCode,
            string districtCode,
            string subDistrictCode,
            string? csvPath = null)
        {
            if (!AddressDataSourceReader.TryGetAddressByCodes(
                countryCode,
                provinceCode,
                districtCode,
                subDistrictCode,
                out var address,
                csvPath))
            {
                return false;
            }

            SetLocationCountry(new AddressCountryValue
            {
                Country = ToResourceValue(address!.Country),
                Province = ToResourceValue(address.Province),
                District = ToResourceValue(address.District),
                SubDistrict = ToResourceValue(address.SubDistrict),
                PostalCode = ToResourceValue(address.PostalCode)
            });

            return true;
        }

        public void SetLocationCountry(AddressCountryValue addressCountryValue)
        {
            Country = addressCountryValue.Country;
            Province = addressCountryValue.Province;
            District = addressCountryValue.District;
            SubDistrict = addressCountryValue.SubDistrict;
            PostalCode = addressCountryValue.PostalCode;
        }

        private static AddressCountryValue? LoadDefaultLocationFromCsv()
        {
            try
            {
                var first = AddressDataSourceReader.ReadThaiAddressLocales().FirstOrDefault();
                if (first is null)
                {
                    return null;
                }

                return new AddressCountryValue
                {
                    Country = ToResourceValue(first.Country),
                    Province = ToResourceValue(first.Province),
                    District = ToResourceValue(first.District),
                    SubDistrict = ToResourceValue(first.SubDistrict),
                    PostalCode = ToResourceValue(first.PostalCode)
                };
            }
            catch
            {
                // Keep constructor safe even when csv is unavailable in some environments.
                return null;
             }
        }

        private static ResourceValue ToResourceValue(AddressDataValueLocale? value)
            => new()
            {
                Code = value?.Code ?? string.Empty,
                DisplayName = value?.DisplayName?.Resolve() ?? string.Empty
            };
    }
}