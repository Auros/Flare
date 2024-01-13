using System.Linq;
using UnityEngine.UIElements;

namespace Flare.Editor.Extensions
{
    public static class UIToolkitExtensions
    {
        public static Toggle GetToggle(this Foldout foldout)
        {
            return (foldout.hierarchy.Children().First() as Toggle)!;
        }
    }
}