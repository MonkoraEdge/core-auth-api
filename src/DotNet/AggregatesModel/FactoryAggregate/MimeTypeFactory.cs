using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.FactoryAggregate
{
    public static class MimeTypeFactory
    {
        public static bool TryFromMimeString(
            string mimeString,
            out MimeType mimeType)
        {
            mimeType = MonkoraEdge.Core.DotNet.Domain.SeedWork.Enumeration
                .GetAll<MimeType>()
                .FirstOrDefault(x =>
                    string.Equals(x.Name, mimeString,
                        StringComparison.OrdinalIgnoreCase));

            return mimeType is not null;
        }
    }
}
