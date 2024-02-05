namespace UnityEngine
{
    internal static class UnityExtensions
    {
        public static T? AsNullable<T>(this T? obj) where T : Object
        {
            return obj ? obj : null;
        }

        public static string GetAnimatablePath(this Transform transform, Transform root)
        {
            if (root == transform)
                return string.Empty;

            string path = string.Empty;
            while (transform != transform.root)
            {
                path = $"{transform.name}/{path}";
                transform = transform.parent;
            }

            if (path.EndsWith('/'))
                path = path[..^1];
            
            return path;
        }
    }
}