using MonkoraEdge.Core.DotNet.Converter;
using Microsoft.Extensions.DependencyInjection;

namespace MonkoraEdge.Core.DotNet.Extensions.Middlewares
{
    public static class ResponseJsonExtensions
    {
        public static IMvcBuilder AddResponseJsonOptions(this IMvcBuilder builder)
        {
            builder.AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
            });

            return builder;
        }
    }
}
