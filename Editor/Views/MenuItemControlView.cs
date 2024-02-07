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

        [PropertyName(nameof(MenuItemInfo.IsSaved))]
        private readonly SerializedProperty _isSavedProperty = null!;
        
        [PropertyName(nameof(MenuItemInfo.DefaultState))]
        private readonly SerializedProperty _defaultStateProperty = null!;
        
        [PropertyName(nameof(MenuItemInfo.DefaultRadialValue))]
        private readonly SerializedProperty _defaultRadialValueProperty = null!;

        [PropertyName(nameof(MenuItemInfo.IsTagTrigger))]
        private readonly SerializedProperty _isTagTriggerProperty = null!;

        [PropertyName(nameof(MenuItemInfo.Tag))]
        private readonly SerializedProperty _tagProperty = null!;
        
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

            var savedField = root.CreatePropertyField(_isSavedProperty)
                .WithTooltip("Is the control value saved between worlds?")
                .WithMarginTop(2f); // (Fix) Toggle property fields are annoyingly smaller than others.
            
            var defaultStateField = root.CreatePropertyField(_defaultStateProperty)
                .WithLabel("Default Toggle Value")
                .WithTooltip("Is this menu item ON or OFF by default?")
                .WithMarginTop(2f); // (Fix) Toggle property fields are annoyingly smaller than others.
            
            var defaultRadialValueField = root.CreatePropertyField(_defaultRadialValueProperty);

            var tagTrigger = root.CreatePropertyField(_isTagTriggerProperty)
                .WithLabel("Is Tag Trigger (Experimental)")
                .WithMarginTop(2f); // (Fix) Toggle property fields are annoyingly smaller than others.
            
            var tagOption = root.CreatePropertyField(_tagProperty)
                .WithLabel("Tag (Experimental)")
                .WithMarginLeft(13f);
            
            tagTrigger.RegisterValueChangeCallback(_ => UpdateAllFieldVisuals(_control.MenuItem.Type));

            UpdateAllFieldVisuals((MenuItemType)_typeProperty.enumValueIndex);
            
            typeField.RegisterValueChangeCallback(ctx =>
                UpdateAllFieldVisuals((MenuItemType)ctx.changedProperty.enumValueIndex)
            );
            
            root.CreateHorizontalSpacer(10f);
            
            return;

            void UpdateAllFieldVisuals(MenuItemType type)
            {
                var isRadial = type is MenuItemType.Radial;
                var isButton = type is MenuItemType.Button;
                
                defaultRadialValueField.style.display = isRadial ? DisplayStyle.Flex : DisplayStyle.None;
                defaultStateField.style.display = isRadial || isButton ? DisplayStyle.None : DisplayStyle.Flex;
                
                tagTrigger.Visible(isButton);
                savedField.Visible(!isButton);
                tagOption.Visible(isButton && _control.MenuItem.IsTagTrigger);
            }
        }
    }
}