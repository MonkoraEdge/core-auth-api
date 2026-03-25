namespace MonkoraEdge.Core.DotNet.AggregatesModel.DataSourceAggregate
{
    public class DataSourceRequest : DataSourcePageRequest
    {
        protected DataSourceRequest()
        {
            this.SortOrders = new List<DataSourceSortOrder>();
        }

        public DataSourceRequest(DataSourcePageRequest dataSourcePageRequest) : base(dataSourcePageRequest)
        {
            this.SortOrders = new List<DataSourceSortOrder>();
        }

        public DataSourceRequest(DataSourceRequest dataSourceRequest) : base(dataSourceRequest)
        {
            this.SortOrders = dataSourceRequest.SortOrders;
        }

        public IList<DataSourceSortOrder> SortOrders { get; set; }
    }
}
