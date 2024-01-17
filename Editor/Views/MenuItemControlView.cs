using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    internal class MenuItemControlView : IView
    {
        [PropertyName(nameof(MenuItemInfo.Name))]
        private readonly SerializedProperty _nameProperty = null!;

        [PropertyName(nameof(MenuItemInfo.Type))]
        private readonly SerializedProperty _typeProperty = null!;

        [PropertyName(nameof(MenuItemInfo.Icon))]
        private readonly SerializedProperty _iconProperty = null!;
        
        [PropertyName(nameof(MenuItemInfo.DefaultState))]
        private readonly SerializedProperty _defaultStateProperty = null!;
        
        [PropertyName(nameof(MenuItemInfo.DefaultRadialValue))]
        private readonly SerializedProperty _defaultRadialValueProperty = null!;
        
        private readonly FlareControl _control;
        
        public MenuItemControlView(FlareControl control) => _control = control;
        
        public void Build(VisualElement root)
        {
            // Menu Item Type
            var typeField = root.CreatePropertyField(_typeProperty)
                .WithLabel("Menu Type");
            
            // Name
            var nameField = root.CreatePropertyField(_nameProperty)
                .WithTooltip("The name of the menu item, used for generating the parameter name and the display in the hand menu.");
            
            nameField.RegisterValueChangeCallback(ctx =>
            {
                if (!_control || _control.Settings.SynchronizeName is false)
                    return;
                
                _control.SetName(ctx.changedProperty.stringValue);
            });
            
            // Icon
            root.CreatePropertyField(_iconProperty)
                .WithTooltip("The icon used when displaying this item in the hand menu.");

            var defaultStateField = root.CreatePropertyField(_defaultStateProperty).WithLabel("Default Toggle Value")
                .WithTooltip("Is this menu item ON or OFF by default?");
            defaultStateField.style.marginTop = 2f; // (Fix) Toggle property fields are annoyingly smaller than others.
            
            var defaultRadialValueField = root.CreatePropertyField(_defaultRadialValueProperty);

            UpdateDefaultStateFields((MenuItemType)_typeProperty.enumValueIndex);
            typeField.RegisterValueChangeCallback(ctx =>
                UpdateDefaultStateFields((MenuItemType)ctx.changedProperty.enumValueIndex)
            );
            
            return;

            void UpdateDefaultStateFields(MenuItemType type)
            {
                var isRadial = type is MenuItemType.Radial;
                defaultRadialValueField.style.display = isRadial ? DisplayStyle.Flex : DisplayStyle.None;
                defaultStateField.style.display = isRadial ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
    }
}