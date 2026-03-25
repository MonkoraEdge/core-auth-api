namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel
{
    public class ErrorField
    {
        public string Field { get; set; }
        public string Error { get; set; }

        public ErrorField(string field, string error)
        {
            Field = field;
            Error = error;
        }
    }
}
