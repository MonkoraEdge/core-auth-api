namespace MonkoraEdge.Core.DotNet.AggregatesModel.DataSourceAggregate
{
    public class DataSourceResponse<T>
    {
        public IEnumerable<T> Data { get; set; }

        public int TotalRecords { get; set; }

        public int RecordsPerPage { get; set; }
    }
}
