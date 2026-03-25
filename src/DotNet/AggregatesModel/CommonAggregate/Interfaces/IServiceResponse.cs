namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces
{
    public interface IServiceResponse
    {
        Guid Id { get; set; }
        bool IsSuccess { get; set; }
        string Message { get; set; }
    }
}
