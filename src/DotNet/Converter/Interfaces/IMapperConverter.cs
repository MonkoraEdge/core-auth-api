namespace MonkoraEdge.Core.DotNet.Converter.Interfaces
{
    public interface IMapperConverter<TSource, TDestination> : IDisposable
    {
        TDestination Map(TSource source);

        List<TDestination> MapList(List<TSource> sources);
    }
}
