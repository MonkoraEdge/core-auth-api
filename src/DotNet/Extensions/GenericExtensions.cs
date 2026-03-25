namespace MonkoraEdge.Core.DotNet.Extensions
{
    public static class GenericExtensions
    {
        public static bool IsICollection(this Type type)
        {
            return Array.Exists(type.GetInterfaces(), IsGenericCollectionType);
        }

        public static bool IsIEnumerable(this Type type)
        {
            return Array.Exists(type.GetInterfaces(), IsGenericEnumerableType);
        }

        public static bool IsIList(this Type type)
        {
            return Array.Exists(type.GetInterfaces(), IsGenericListType);
        }

        private static bool IsGenericCollectionType(Type type)
        {
            return type.IsGenericType && (typeof(ICollection<>) == type.GetGenericTypeDefinition());
        }

        private static bool IsGenericEnumerableType(Type type)
        {
            return type.IsGenericType && (typeof(IEnumerable<>) == type.GetGenericTypeDefinition());
        }

        private static bool IsGenericListType(Type type)
        {
            return type.IsGenericType && (typeof(IList<>) == type.GetGenericTypeDefinition());
        }
    }
}
