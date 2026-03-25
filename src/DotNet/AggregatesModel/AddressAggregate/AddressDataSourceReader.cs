using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AddressAggregate
{
    public static class AddressDataSourceReader
    {
        private const string ThaiAddressRelativePath = "DataSources/thai_address.csv";
        private static readonly Lazy<IReadOnlyList<AddressCountryLocale>> _cachedThaiAddressLocales =
            new(() => ReadThaiAddressLocalesCore(ResolveThaiAddressPath(null)));

        public static IReadOnlyList<AddressCountryLocale> ReadThaiAddressLocales(string? csvPath = null)
            => string.IsNullOrWhiteSpace(csvPath)
                ? _cachedThaiAddressLocales.Value
                : ReadThaiAddressLocalesCore(ResolveThaiAddressPath(csvPath));

        public static IReadOnlyList<AddressDataValueLocale> GetCountries(string? csvPath = null)
            => DistinctByCode(ReadThaiAddressLocales(csvPath).Select(x => x.Country));

        public static IReadOnlyList<AddressDataValueLocale> GetProvincesByCountry(string countryCode, string? csvPath = null)
        {
            var normalized = NormalizeCode(countryCode);
            return DistinctByCode(
                ReadThaiAddressLocales(csvPath)
                    .Where(x => NormalizeCode(x.Country?.Code) == normalized)
                    .Select(x => x.Province));
        }

        public static IReadOnlyList<AddressDataValueLocale> GetDistrictsByProvince(string provinceCode, string? csvPath = null)
        {
            var normalized = NormalizeCode(provinceCode);
            return DistinctByCode(
                ReadThaiAddressLocales(csvPath)
                    .Where(x => NormalizeCode(x.Province?.Code) == normalized)
                    .Select(x => x.District));
        }

        public static IReadOnlyList<AddressDataValueLocale> GetSubDistrictsByDistrict(string districtCode, string? csvPath = null)
        {
            var normalized = NormalizeCode(districtCode);
            return DistinctByCode(
                ReadThaiAddressLocales(csvPath)
                    .Where(x => NormalizeCode(x.District?.Code) == normalized)
                    .Select(x => x.SubDistrict));
        }

        public static IReadOnlyList<AddressDataValueLocale> GetPostalCodesBySubDistrict(string subDistrictCode, string? csvPath = null)
        {
            var normalized = NormalizeCode(subDistrictCode);
            return DistinctByCode(
                ReadThaiAddressLocales(csvPath)
                    .Where(x => NormalizeCode(x.SubDistrict?.Code) == normalized)
                    .Select(x => x.PostalCode));
        }

        public static bool TryGetAddressByCodes(
            string countryCode,
            string provinceCode,
            string districtCode,
            string subDistrictCode,
            out AddressCountryLocale? address,
            string? csvPath = null)
        {
            var country = NormalizeCode(countryCode);
            var province = NormalizeCode(provinceCode);
            var district = NormalizeCode(districtCode);
            var subDistrict = NormalizeCode(subDistrictCode);

            address = ReadThaiAddressLocales(csvPath)
                .FirstOrDefault(x =>
                    NormalizeCode(x.Country?.Code) == country &&
                    NormalizeCode(x.Province?.Code) == province &&
                    NormalizeCode(x.District?.Code) == district &&
                    NormalizeCode(x.SubDistrict?.Code) == subDistrict);

            return address is not null;
        }

        private static IReadOnlyList<AddressCountryLocale> ReadThaiAddressLocalesCore(string resolvedPath)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim
            };

            using var stream = File.OpenRead(resolvedPath);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, config);

            csv.Context.RegisterClassMap<AddressResponseMap>();

            return csv
                .GetRecords<AddressCountryLocale>()
                .Where(x => x is not null)
                .ToList();
        }

        private static IReadOnlyList<AddressDataValueLocale> DistinctByCode(IEnumerable<AddressDataValueLocale> values)
            => values
                .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => NormalizeCode(x.Code))
                .Select(g => g.First())
                .ToList();

        private static string NormalizeCode(string? code)
            => code?.Trim().ToUpperInvariant() ?? string.Empty;

        internal static string ResolveThaiAddressPath(string? csvPath)
        {
            if (!string.IsNullOrWhiteSpace(csvPath))
            {
                if (!File.Exists(csvPath))
                    throw new FileNotFoundException($"Thai address CSV not found at '{csvPath}'.", csvPath);

                return csvPath;
            }

            var baseDir = AppContext.BaseDirectory;
            for (var i = 0; i < 8; i++)
            {
                var candidate = Path.Combine(baseDir, ThaiAddressRelativePath);
                if (File.Exists(candidate))
                    return candidate;

                var parent = Directory.GetParent(baseDir);
                if (parent is null)
                    break;

                baseDir = parent.FullName;
            }

            throw new FileNotFoundException(
                $"Unable to locate '{ThaiAddressRelativePath}'. Provide an explicit csvPath.",
                ThaiAddressRelativePath);
        }
    }
}
