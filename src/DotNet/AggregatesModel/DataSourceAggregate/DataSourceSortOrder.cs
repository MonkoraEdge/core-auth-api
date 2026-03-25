using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.DataSourceAggregate
{
    public class DataSourceSortOrder
    {
        public string Field { get; set; }
        public DataSourceSortOrderDirection Direction { get; set; }

        public string ToExpression() => this.Field + " " + this.Direction.ToString().ToUpper();
    }
}
