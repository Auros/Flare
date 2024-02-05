using UnityEditor;

namespace Flare.Editor.Models
{
    internal interface IFlareBindable
    {
        void SetBinding(SerializedProperty property);
    }
}