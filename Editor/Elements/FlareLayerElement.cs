using Flare.Editor.Extensions;
using Flare.Editor.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Elements
{
    public class FlareLayerElement : VisualElement, IFlareBindable
    {
        public FlareLayerElement()
        {
            this.CreateLabel("2TD");
        }
        
        public void SetBinding(SerializedProperty property)
        {
            
        }
    }
}