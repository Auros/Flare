using System.Collections.Generic;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ObjectToggleInfo))]
    public class ObjectToggleInfoPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            var objectField = root.CreatePropertyField(property.Property(nameof(ObjectToggleInfo.Context))!)
                .WithLabel(string.Empty);
            
            // This prevents PropertyFields from auto-adjusting the width of the labels
            // We don't want to do this for the Object property to make it more obvious
            // where to add the object.
            objectField.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (objectField.childCount is 0)
                    return;

                // Remove alignment marker.
                var childObjectField = objectField.Q<ObjectField>();
                childObjectField.RemoveFromClassList(ObjectField.alignedFieldUssClassName);
                
                // Reset label width to zero.
                var label = childObjectField.Q<Label>();
                label.style.width = 0;
            });

            var horizontal = root.CreateHorizontal().WithMinHeight(24f).WithMaxHeight(192f);
            horizontal.style.flexShrink = 1f;
            horizontal.style.flexWrap = Wrap.Wrap;
            
            horizontal.CreateLabel("This").WithHeight(20f).WithTextAlign(TextAnchor.MiddleLeft);
            var ddf = new DropdownField(new List<string> { "GameObject", "VRCPhysBone", "ParentConstraint" }, 0)
                .WithHeight(20f);
            horizontal.Add(ddf);
            
            horizontal.CreateLabel("  should be").WithHeight(20f).WithTextAlign(TextAnchor.MiddleLeft);
            var defState = new EnumField(ToggleMode.Enabled).WithHeight(20f);
            horizontal.Add(defState);
            
            horizontal.CreateLabel("  when this menu item is").WithHeight(20f).WithTextAlign(TextAnchor.MiddleLeft);
            var menuState = new EnumField(ToggleMode.Disabled).WithHeight(20f);
            horizontal.Add(menuState);
            
            return root;
        }
    }
}