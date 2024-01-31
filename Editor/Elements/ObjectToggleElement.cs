using System;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Flare.Editor.Elements
{
    public class ObjectToggleElement : VisualElement
    {
        private readonly PaneMenu _paneMenu;
        private readonly ObjectField _objectField;
        private readonly EnumField _menuModeField;
        private readonly EnumField _toggleModeField;
        private readonly VisualElement _builderContainer;
        private readonly ComponentDropdownField _componentDropdownField;
        
        public ObjectToggleElement(GameObject? context = null)
        {
            // Create initial editable fields
            _paneMenu = new PaneMenu();
            _objectField = new ObjectField();
            _menuModeField = new EnumField(ToggleMode.Disabled);
            _toggleModeField = new EnumField(ToggleMode.Disabled);
            _componentDropdownField = new ComponentDropdownField(context);
            
            // Register value change callbacks
            _objectField.RegisterValueChangedCallback(OnObjectFieldChanged);
            _menuModeField.RegisterValueChangedCallback(OnMenuModeFieldChange);
            _toggleModeField.RegisterValueChangedCallback(OnToggleModeFieldChanged);
            
            _objectField.WithGrow(1f);
            _menuModeField.WithHeight(20f);
            _toggleModeField.WithHeight(20f);
            _componentDropdownField.WithHeight(20f);
            _paneMenu.WithWidth(10f).WithHeight(20f);
            _paneMenu.style.marginLeft = 5f;
            
            // Create the top shelf with the object picker and context menu.
            var topContainer = this.CreateHorizontal();
            topContainer.Add(_objectField);
            topContainer.Add(_paneMenu);

            // Add the object field for picking toggle object.
            Add(topContainer);
            
            // Setup the toggle builder container
            var root = this.CreateHorizontal()
                .WithWrap(Wrap.Wrap)
                .WithMaxHeight(192f)
                .WithMinHeight(24f)
                .WithShrink(1f);

            // Add text: "This"
            root.CreateLabel("This")
                .WithTextAlign(TextAnchor.MiddleLeft)
                .WithHeight(20f);
            
            // Add the component dropdown
            root.Add(_componentDropdownField);
            
            // Add text: "should be"
            root.CreateLabel("  should be")
                .WithTextAlign(TextAnchor.MiddleLeft)
                .WithHeight(20f);
            
            // Add the toggle mode dropdown
            root.Add(_toggleModeField);
            
            // Add text: "when this menu item is"
            root.CreateLabel("  when this menu item is")
                .WithTextAlign(TextAnchor.MiddleLeft)
                .WithHeight(20f);
            
            // Add the menu mode dropdown
            root.Add(_menuModeField);

            _builderContainer = root;
        }

        private void OnMenuModeFieldChange(ChangeEvent<Enum> evt) => OnModeFieldChanged(_menuModeField, evt);

        private void OnToggleModeFieldChanged(ChangeEvent<Enum> evt) => OnModeFieldChanged(_toggleModeField, evt);

        private static void OnModeFieldChanged(EnumField field, ChangeEvent<Enum> evt)
        {
            OnModeFieldChanged(field, (ToggleMode)(evt.newValue ?? evt.previousValue));
        }
        
        // ReSharper disable once SuggestBaseTypeForParameter
        private static void OnModeFieldChanged(EnumField field, ToggleMode value)
        {
            // Color the enum dropdown text according to the value of the toggle mode.
            var enabledColor = (Color)new Color32(0x5B, 0xDD, 0x55, 0xFF);
            var disabledColor = (Color)new Color32(0xFF, 0x7C, 0x7C, 0xFF);
            
            var targetColor = value is ToggleMode.Enabled ? enabledColor : disabledColor;
            
            field.Q<TextElement>().WithColor(targetColor);
        }

        private void OnObjectFieldChanged(ChangeEvent<Object> evt)
        {
            // If the object field changed, we need to update the children
            // If it's null, we hide the toggle builder, otherwise show it.
            // If the object changes, we also need to update the component dropdown options.
            _builderContainer.Visible(evt.newValue).Enabled(evt.newValue);
            _componentDropdownField.Push(evt.newValue);
            
            // Ensure that the enum dropdowns are colored properly when initializing new toggle objects.
            // Without this sometimes they won't be colored.
            if (_menuModeField.value is ToggleMode menuMode)
                OnModeFieldChanged(_menuModeField, menuMode);
            
            if (_toggleModeField.value is ToggleMode toggleMode)
                OnModeFieldChanged(_toggleModeField, toggleMode);
        }

        /// <summary>
        /// Set and bind a serialized property to this toggle element.
        /// </summary>
        /// <param name="property">A serialized property of type <see cref="ObjectToggleInfo"/></param>
        /// <param name="onRemoveRequested">Called when the a removal has been requested.</param>
        public void SetData(SerializedProperty property, Action? onRemoveRequested = null)
        {
            _objectField.Unbind();
            _menuModeField.Unbind();
            _toggleModeField.Unbind();
            _componentDropdownField.Unbind();
            
            _objectField.BindProperty(property.Property(nameof(ObjectToggleInfo.Target)));
            _menuModeField.BindProperty(property.Property(nameof(ObjectToggleInfo.MenuMode)));
            _toggleModeField.BindProperty(property.Property(nameof(ObjectToggleInfo.ToggleMode)));
            _componentDropdownField.BindProperty(property.Property(nameof(ObjectToggleInfo.Target)));
            
            _paneMenu.SetData(evt => evt.menu.AppendAction("Remove Toggle", _ => onRemoveRequested?.Invoke()));
        }
    }
}