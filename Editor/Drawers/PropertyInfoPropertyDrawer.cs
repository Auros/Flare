using Flare.Editor.Extensions;
using Flare.Editor.Windows;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(PropertyInfo))]
    public class PropertyInfoPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            
            root.Add(new Button(() => OnPropertyRequestClick(property)) { text = "Select Property" });
            var pathField = root.CreatePropertyField(property.Property(nameof(PropertyInfo.Path)));
            pathField.SetEnabled(false);
            
            return root;
        }

        private static void OnPropertyRequestClick(SerializedProperty property)
        {
            PropertySelectorWindow.Present(property);
        }
    }
}