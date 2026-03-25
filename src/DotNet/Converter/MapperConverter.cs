using AutoMapper;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.Converter.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace MonkoraEdge.Core.DotNet.Converter
{
    public class MapperConverter<TSource, TDestination> : IMapperConverter<TSource, TDestination>
    {
        private IMapper _mapper;
        public MapperConverter(MapperProfile<TSource, TDestination> mapperProfile)
        {
            var expr = new MapperConfigurationExpression();
            expr.AddProfile(mapperProfile);
            var config = new MapperConfiguration(expr, loggerFactory: NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        #region Dispose
        // Example of an unmanaged resource
        private nint unmanagedResource;
        private bool _disposed = false;

        // Destructor (Finalizer) to catch cases where Dispose wasn't called
        ~MapperConverter() => Dispose(false);

        // Public implementation of Dispose pattern
        // Synchronous cleanup (IDisposable implementation)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Suppresses finalization since resources are already cleaned up
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources.
                // if use new object class in this . Dispos object is here
                // TODO: dispose managed state (managed objects)
                // For example: managedResource.Dispose();
            }

            // Free unmanaged resources
            // Call the appropriate methods to clean up 
            // unmanaged resources here.
            // ex. Boolean , DataTable
            if (unmanagedResource != nint.Zero)
            {
                // Free unmanaged resource, e.g., Marshal.FreeHGlobal(unmanagedResource);
                unmanagedResource = nint.Zero;
            }

            _disposed = true;
        }

        #endregion Dispose

        public TDestination Map(TSource source)
        {
            if (_mapper == null) throw new InvalidOperationException("Mapper not initialized. Call Initialize() first.");

            if (source == null) throw new ArgumentNullException(nameof(source));

            return _mapper.Map<TDestination>(source);
        }

        public List<TDestination> MapList(List<TSource> sources)
        {
            if (_mapper == null) throw new InvalidOperationException("Mapper not initialized. Call Initialize() first.");

            if (sources == null) throw new ArgumentNullException(nameof(sources));

            return _mapper.Map<List<TDestination>>(sources);
        }
    }
}
