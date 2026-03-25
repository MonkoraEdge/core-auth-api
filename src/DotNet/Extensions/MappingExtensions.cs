
namespace MonkoraEdge.Core.DotNet.Extensions
{
    //Application
    public static class MappingExtensions
    {
        public static TTarget MapTo<TTarget>(this object source)
            where TTarget : new()
        {
            var target = new TTarget();

            var sourceProps = source.GetType().GetProperties();
            var targetProps = typeof(TTarget).GetProperties();

            foreach (var prop in sourceProps)
            {
                var targetProp = targetProps.FirstOrDefault(x => x.Name == prop.Name);
                if (targetProp != null && targetProp.CanWrite)
                {
                    var value = prop.GetValue(source);
                    targetProp.SetValue(target, value);
                }
            }

            return target;
        }
    }
}
