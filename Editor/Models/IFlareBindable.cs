using UnityEditor;

namespace Flare.Editor.Models
{
    public interface IFlareBindable
    {
        void SetBinding(SerializedProperty property);
    }
}