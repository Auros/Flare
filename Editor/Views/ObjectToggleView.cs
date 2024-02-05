using System;
using Flare.Editor.Attributes;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    internal class ObjectToggleView : IView
    {
        [PropertyName("Array")]
        private readonly SerializedProperty _arrayProperty = null!;
        
        [PropertyName("Array.size")]
        private readonly SerializedProperty _arraySizeProperty = null!;

        private readonly FlareControl _flareControl;
        
        public ObjectToggleView(FlareControl flareControl)
        {
            _flareControl = flareControl;
        }
        
        public void Build(VisualElement root)
        {
            var container = root.CreateElement();
            root.TrackPropertyValue(_arraySizeProperty, _ => BuildToggleView(container));
            BuildToggleView(container);
            root.CreateButton("Add New Toggle", () =>
            {
                var index = _arrayProperty.arraySize++;
                _arrayProperty.serializedObject.ApplyModifiedProperties();
                
                ObjectToggleInfo info = new();
                
                // For menus, we seed some initial values to assume what the user wants.
                if (_flareControl.Type is ControlType.Menu)
                {
                    var menuItem = _flareControl.MenuItem;
                    var state = menuItem.Type switch
                    {
                        MenuItemType.Button => ToggleMode.Enabled,
                        MenuItemType.Toggle => menuItem.DefaultState ? ToggleMode.Disabled : ToggleMode.Enabled,
                        MenuItemType.Radial => menuItem.DefaultRadialValue >= 0.5f ? ToggleMode.Disabled : ToggleMode.Enabled,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    info.MenuMode = state;
                }
                else
                {
                    info.MenuMode = ToggleMode.Enabled;
                }
                
                _arrayProperty.GetArrayElementAtIndex(index).SetValue(info);
            });
        }

        private static ObjectToggleElement CreateObjectToggleElement()
        {
            ObjectToggleElement root = new();

            root.WithBackgroundColor(FlareUI.BackgroundColor)
                .WithBorderColor(FlareUI.BorderColor)
                .WithBorderRadius(3f)
                .WithBorderWidth(1f)
                .WithMarginTop(5f)
                .WithPadding(5f);
            
            return root;
        }

        private void BuildToggleView(VisualElement container)
        {
            var index = 0;
            var endProperty = _arrayProperty.GetEndProperty();

            // Copy the array property to iterate over it.
            var property = _arrayProperty.Copy();

            // Look at the next property.
            property.NextVisible(true);

            do // If I had a dollar for every time I used a do while loop in a project, I would have $1
            {
                // Stop looking after the end property
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;
                
                // Ignore array size properties.
                if (property.propertyType is SerializedPropertyType.ArraySize)
                    continue;

                ObjectToggleElement element;
                if (index >= container.childCount)
                {
                    // Add new elements as the array has grown
                    element = CreateObjectToggleElement();
                    container.Add(element);
                }
                else
                {
                    // We don't need to resize for this property.
                    element = (ObjectToggleElement)container[index];
                }

                var targetIndex = index;
                element.SetData(property, () =>
                {
                    _arrayProperty.DeleteArrayElementAtIndex(targetIndex);
                    _arrayProperty.serializedObject.ApplyModifiedProperties();
                });
                index++;
            }
            while (property.NextVisible(false));

            // Remove any extra elements.
            while (container.childCount > index)
                container.RemoveAt(container.childCount - 1);
        }
    }
}