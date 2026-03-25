namespace MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate
{
    public static class PhoneRegex
    {
        public const string Thailand = @"^0\d{8,9}$";
        public const string International = @"^\+\d{6,15}$";
        public const string US = @"^\+1\d{10}$";
        public const string UK = @"^\+44\d{10}$";
        public const string China = @"^\+86\d{11}$";
        public const string Japan = @"^\+81\d{9,10}$";
    }
}
