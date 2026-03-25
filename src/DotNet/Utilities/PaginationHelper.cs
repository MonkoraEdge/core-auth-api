namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class PaginationHelper
    {
        public static int GetSkip(int page, int size) => (page - 1) * size;
    }
}
