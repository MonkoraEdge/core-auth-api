namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces
{
    public interface ILocalization
    {
        string GetText(string key);

        string GetText(string key, string culture);
    }
}
