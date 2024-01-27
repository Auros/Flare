using System;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Flare.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ObjectToggleInfo))]
    public class ObjectToggleInfoPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();

            var context = property.Property(nameof(ObjectToggleInfo.Target)).objectReferenceValue as GameObject;
            var objectToggleInfoProperty = property.Property(nameof(ObjectToggleInfo.Target));
            var objectField = root.CreatePropertyField(objectToggleInfoProperty)
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
            objectField.RegisterValueChangeCallback(ctx =>
            {
                var newObj = ctx.changedProperty.objectReferenceValue;
                if (ctx.target is not VisualElement element)
                    return;
                
                var toggleQuery = element.parent.Q<VisualElement>("ToggleQuery");
                UpdateToggleQuery(toggleQuery, newObj);

                var dropdown = toggleQuery.Q<ComponentDropdownField>();
                dropdown.Push(newObj);
                
            });

            var horizontal = root.CreateHorizontal()
                .WithMaxHeight(192f)
                .WithMinHeight(24f);

            root.style.marginBottom = 5;
            
            UpdateToggleQuery(horizontal, context);
            horizontal.style.flexWrap = Wrap.Wrap;
            horizontal.style.flexShrink = 1f;
            
            horizontal.CreateLabel("This")
                .WithTextAlign(TextAnchor.MiddleLeft)
                .WithHeight(20f);

            ComponentDropdownField componentDropdownField = new(context);
            componentDropdownField.WithHeight(20f);
            horizontal.Add(componentDropdownField);
            horizontal.name = "ToggleQuery";
            componentDropdownField.BindTo(property.Property(nameof(ObjectToggleInfo.Target)));
            
            horizontal.CreateLabel("  should be")
                .WithTextAlign(TextAnchor.MiddleLeft)
                .WithHeight(20f);
            
            var toggleModeField = horizontal.CreateEnumField(ToggleMode.Disabled)
                .BindTo(property.Property(nameof(ObjectToggleInfo.ToggleMode)))
                .WithHeight(20f);
            
            horizontal.CreateLabel("  when this menu item is")
                .WithTextAlign(TextAnchor.MiddleLeft)
                .WithHeight(20f);
            
            var menuModeField = horizontal.CreateEnumField(ToggleMode.Disabled)
                .BindTo(property.Property(nameof(ObjectToggleInfo.MenuMode)))
                .WithHeight(20f);

            UpdateModeFieldColor(menuModeField, (ToggleMode)property.Property(nameof(ObjectToggleInfo.MenuMode)).enumValueIndex);
            UpdateModeFieldColor(toggleModeField, (ToggleMode)property.Property(nameof(ObjectToggleInfo.ToggleMode)).enumValueIndex);
            
            menuModeField.RegisterValueChangedCallback(UpdateToggleCallback);
            toggleModeField.RegisterValueChangedCallback(UpdateToggleCallback);
            
            void UpdateToggleCallback(ChangeEvent<Enum> ctx)
            {
                var value = (ToggleMode)(ctx.newValue ?? ctx.previousValue);
                UpdateModeFieldColor(ctx.target, value);
            }
            
            void UpdateModeFieldColor(IEventHandler field, ToggleMode mode)
            {
                var badColor = (Color)new Color32(0xFF, 0x7C, 0x7C, 0xFF);
                var goodColor = (Color)new Color32(0x5B, 0xDD, 0x55, 0xFF);

                if (field is not VisualElement element)
                    return;
                
                element.Q<TextElement>().style.color = mode is ToggleMode.Enabled ? goodColor : badColor;
            }

            void UpdateToggleQuery(VisualElement element, Object? ctx)
            {
                element.SetEnabled(ctx != null);
                element.style.display = ctx != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            return root;
        }
    }
}