namespace MonkoraEdge.Core.DotNet.AggregatesModel.DataSourceAggregate
{
    public class DataSourcePageRequest
    {
        private int _page;
        private int _recordsPerPage;

        protected DataSourcePageRequest()
        {

        }

        protected DataSourcePageRequest(DataSourcePageRequest dataSourcePageRequest)
        {
            this.Page = dataSourcePageRequest.Page;
            this.RecordsPerPage = dataSourcePageRequest.RecordsPerPage;
        }

        public int Page
        {
            get => this._page <= 0 ? 1 : this._page;
            set => this._page = value;
        }

        public int RecordsPerPage
        {
            get => this._recordsPerPage <= 0 ? 20 : this._recordsPerPage;
            set => this._recordsPerPage = value;
        }
    }
}
