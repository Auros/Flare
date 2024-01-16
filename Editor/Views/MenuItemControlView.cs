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
        
        private readonly FlareControl _control;
        
        public MenuItemControlView(FlareControl control) => _control = control;
        
        public void Build(VisualElement root)
        {
            // Menu Item Type
            root.CreatePropertyField(_typeProperty)
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
            var i = root.CreatePropertyField(_iconProperty)
                .WithTooltip("The icon used when displaying this item in the hand menu.");
        }
    }
}