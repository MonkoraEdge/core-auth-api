using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate
{
    public class CreateResponse : IServiceResponse
    {
        public Guid Id { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
