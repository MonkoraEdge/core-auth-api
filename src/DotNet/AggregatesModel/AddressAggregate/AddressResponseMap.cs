using CsvHelper.Configuration;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.AddressAggregate
{
    public sealed class AddressResponseMap : ClassMap<AddressCountryLocale>
    {
        public AddressResponseMap()
        {
            Map(m => m.Country.Code).Name("Country/Code");
            Map(m => m.Country.DisplayName.EN).Name("Country/DisplayName/EN");
            Map(m => m.Country.DisplayName.TH).Name("Country/DisplayName/TH");
            Map(m => m.Country.DisplayName.ZH).Convert(args =>
                TryGetField(args.Row, "Country/DisplayName/ZH", args.Row.GetField("Country/DisplayName/EN")));

            Map(m => m.Province.Code).Name("Province/Code");
            Map(m => m.Province.DisplayName.EN).Name("Province/DisplayName/EN");
            Map(m => m.Province.DisplayName.TH).Name("Province/DisplayName/TH");
            Map(m => m.Province.DisplayName.ZH).Convert(args =>
                TryGetField(args.Row, "Province/DisplayName/ZH", args.Row.GetField("Province/DisplayName/EN")));

            Map(m => m.District.Code).Name("District/Code");
            Map(m => m.District.DisplayName.EN).Name("District/DisplayName/EN");
            Map(m => m.District.DisplayName.TH).Name("District/DisplayName/TH");
            Map(m => m.District.DisplayName.ZH).Convert(args =>
                TryGetField(args.Row, "District/DisplayName/ZH", args.Row.GetField("District/DisplayName/EN")));

            Map(m => m.SubDistrict.Code).Name("SubDistrict/Code", "Subdistrict/Code");
            Map(m => m.SubDistrict.DisplayName.EN).Name("SubDistrict/DisplayName/EN", "Subdistrict/DisplayName/EN");
            Map(m => m.SubDistrict.DisplayName.TH).Name("SubDistrict/DisplayName/TH", "Subdistrict/DisplayName/TH");
            Map(m => m.SubDistrict.DisplayName.ZH).Convert(args =>
                TryGetField(args.Row, "SubDistrict/DisplayName/ZH",
                    TryGetField(args.Row, "Subdistrict/DisplayName/ZH",
                        TryGetField(args.Row, "SubDistrict/DisplayName/EN", args.Row.GetField("Subdistrict/DisplayName/EN")))));

            Map(m => m.PostalCode.Code).Name("PostalCode/Code");
            Map(m => m.PostalCode.DisplayName.EN).Name("PostalCode/DisplayName/EN");
            Map(m => m.PostalCode.DisplayName.TH).Name("PostalCode/DisplayName/TH");
            Map(m => m.PostalCode.DisplayName.ZH).Convert(args =>
                TryGetField(args.Row, "PostalCode/DisplayName/ZH", args.Row.GetField("PostalCode/DisplayName/EN")));
        }

        private static string TryGetField(CsvHelper.IReaderRow row, string fieldName, string fallback)
            => row.TryGetField<string>(fieldName, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
    }
}