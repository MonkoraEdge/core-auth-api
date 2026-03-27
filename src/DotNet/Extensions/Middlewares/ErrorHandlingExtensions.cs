using Microsoft.AspNetCore.Builder;
using MonkoraEdge.Core.DotNet.Middleware;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.Extensions.Middlewares
{
    public static class ErrorHandlingExtensions
    {
        //public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder, ErrorHandlingOptions errorHandlingOptions = null)
        //{
        //    return errorHandlingOptions is null ? builder.UseMiddleware(typeof(ErrorHandlingMiddleware)) :
        //        builder.UseMiddleware(typeof(ErrorHandlingMiddleware), errorHandlingOptions);
        //}

        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder, ErrorHandlingOptions errorHandlingOptions = null)
        {
            _ = errorHandlingOptions;
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
