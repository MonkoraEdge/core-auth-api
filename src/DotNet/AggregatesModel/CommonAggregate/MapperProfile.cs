using AutoMapper;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate
{
    public class MapperProfile<TSource, TDestination> : Profile
    {
        public virtual MapperProfile<TSource, TDestination> GenerateProfile(Action<TSource, TDestination> customMapping)
        {
            CreateMap<TSource, TDestination>()
                .AfterMap((src, dest) => { customMapping?.Invoke(src, dest); });

            return this;
        }
    }
}
