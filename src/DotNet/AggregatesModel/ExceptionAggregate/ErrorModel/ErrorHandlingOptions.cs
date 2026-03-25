namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel
{
    public class ErrorHandlingOptions
    {
        public string ServiceName { get; }

        public ErrorHandlingOptions(string serviceName)
        {
            ServiceName = serviceName;
        }
    }
}