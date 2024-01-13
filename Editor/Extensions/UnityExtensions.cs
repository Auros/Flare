namespace UnityEngine
{
    internal static class UnityExtensions
    {
        public static T? AsNullable<T>(this T? obj) where T : Object
        {
            return obj ? obj : null;
        }
    }
}