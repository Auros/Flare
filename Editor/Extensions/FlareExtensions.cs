using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;

namespace Flare.Editor.Extensions
{
    internal static class FlareExtensions
    {
        public static DeclaringPass Run<T>(this Sequence sequence) where T : Pass<T>, new()
        {
            return sequence.Run(new T());
        }
    }
}