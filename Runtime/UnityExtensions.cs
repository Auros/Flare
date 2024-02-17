using System;

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
        
        public static Object? GetObjectAtPath(this Transform root, Type type, string path)
        {
            if (path == string.Empty)
                return root.GetComponent(type);
            
            var target = root;
            foreach (var part in path.Split('/'))
            {
                for (int i = 0; i < target.childCount; i++)
                {
                    var child = target.GetChild(i);
                    if (child.name != part)
                        continue;

                    target = child;
                }
            }

            return path != target.GetAnimatablePath(root) ? null
                : (type == typeof(GameObject) ? target.gameObject : target.GetComponent(type));
        }
    }
}