using System;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Flare.Editor.Elements
{
    internal class ObjectToggleElement : VisualElement
    {
        private readonly PaneMenu _paneMenu;
        private readonly ObjectField _objectField;
        private readonly EnumField _menuModeField;
        private readonly EnumField _toggleModeField;
        private readonly EnumField _alternateModeField;
        private readonly EnumField _alternateModeDisplay;
        private readonly VisualElement _builderContainer;
        private readonly ComponentDropdownField _componentDropdownField;

        private readonly VisualElement[] _overrideElements;
        
        public ObjectToggleElement(GameObject? context = null)
        {
            // Create initial editable fields
            _paneMenu = new PaneMenu();
            _objectField = new ObjectField();
            _menuModeField = new EnumField();
            _toggleModeField = new EnumField();
            _alternateModeField = new EnumField();
            _alternateModeDisplay = new EnumField(ToggleMenuState.Inactive);
            _componentDropdownField = new ComponentDropdownField(context);
            
            // Register value change callbacks
            _objectField.RegisterValueChangedCallback(OnObjectFieldChanged);
            _menuModeField.RegisterValueChangedCallback(OnMenuModeFieldChange);
            _toggleModeField.RegisterValueChangedCallback(OnToggleModeFieldChanged);
            _alternateModeDisplay.RegisterValueChangedCallback(OnOverrideMenuModeFieldChange);
            _alternateModeField.RegisterValueChangedCallback(OnOverrideToggleModeFieldChanged);
            
            _objectField.WithGrow(1f);
            _menuModeField.WithHeight(20f);
            _toggleModeField.WithHeight(20f);
            _alternateModeField.WithHeight(20f);
            _alternateModeDisplay.WithHeight(20f);
            _componentDropdownField.WithHeight(20f);
            _paneMenu.WithWidth(10f).WithHeight(20f);
            _alternateModeDisplay.Enabled(false);
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
                .WithMaxHeight(230f)
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

            var alt0 = root.CreateLabel(" ,")
                .WithTextAlign(TextAnchor.MiddleLeft)
                .WithHeight(20f);

            var alt1 = root.CreateLabel("and it should be")
                .WithTextAlign(TextAnchor.MiddleLeft)
                .WithHeight(20f);
            
            root.Add(_alternateModeField);
            
            var alt2 = root.CreateLabel("  when the menu item is")
                .WithTextAlign(TextAnchor.MiddleLeft)
                .WithHeight(20f);
            
            root.Add(_alternateModeDisplay);

            _overrideElements = new VisualElement[]
            {
                alt0,
                alt1,
                alt2,
                _alternateModeField,
                _alternateModeDisplay
            };
                
            _builderContainer = root;
        }

        private void OnMenuModeFieldChange(ChangeEvent<Enum> evt) => OnModeFieldChanged(_menuModeField, evt);

        private void OnToggleModeFieldChanged(ChangeEvent<Enum> evt) => OnModeFieldChanged(_toggleModeField, evt);
        
        private void OnOverrideMenuModeFieldChange(ChangeEvent<Enum> evt) => OnModeFieldChanged(_alternateModeDisplay, evt);

        private void OnOverrideToggleModeFieldChanged(ChangeEvent<Enum> evt) => OnModeFieldChanged(_alternateModeField, evt);

        private static void OnModeFieldChanged(EnumField field, ChangeEvent<Enum> evt)
        {
            OnModeFieldChanged(field, (ToggleMode)(evt.newValue ?? evt.previousValue));
        }
        
        // ReSharper disable once SuggestBaseTypeForParameter
        private static void OnModeFieldChanged(EnumField field, ToggleMode value)
        {
            // Color the enum dropdown text according to the value of the toggle mode.
            var enabledColor = FlareUI.EnabledColor;
            var disabledColor = FlareUI.DisabledColor;
            
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
            
            if (_alternateModeField.value is ToggleMode altMenuMode)
                OnModeFieldChanged(_alternateModeField, altMenuMode);
            
            if (_alternateModeDisplay.value is ToggleMode altToggleMode)
                OnModeFieldChanged(_alternateModeDisplay, altToggleMode);
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
            _alternateModeField.Unbind();
            _componentDropdownField.Unbind();

            var menuModeProperty = property.Property(nameof(ObjectToggleInfo.MenuMode));
            var altModeProperty = property.Property(nameof(ObjectToggleInfo.AlternateMode));
            var overrideProperty = property.Property(nameof(ObjectToggleInfo.OverrideDefaultValue)).Copy();

            _objectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.previousValue != null || evt.newValue == null)
                    return;

                bool? value = evt.newValue switch
                {
                    GameObject go => go.activeSelf,
                    Behaviour behaviour => behaviour.enabled,
                    Renderer renderer => renderer.enabled,
                    _ => null
                };

                if (!value.HasValue)
                    return;
                
                _toggleModeField.value = value.Value ? ToggleMode.Disabled : ToggleMode.Enabled;
            });

            _objectField.TrackPropertyValue(overrideProperty, _ =>
            {
                foreach (var component in _overrideElements)
                    component.Visible(overrideProperty.boolValue);
            });
            _alternateModeField.TrackPropertyValue(menuModeProperty, _ =>
            {
                _alternateModeDisplay.value = (ToggleMenuState)menuModeProperty.enumValueIndex is ToggleMenuState.Active
                    ? ToggleMenuState.Inactive : ToggleMenuState.Active;
                
                foreach (var component in _overrideElements)
                    component.Visible(overrideProperty.boolValue);
            });

            foreach (var component in _overrideElements)
                component.Visible(overrideProperty.boolValue);
            
            _alternateModeDisplay.value = (ToggleMenuState)menuModeProperty.enumValueIndex is ToggleMenuState.Active
                ? ToggleMenuState.Inactive : ToggleMenuState.Active;
            
            _objectField.BindProperty(property.Property(nameof(ObjectToggleInfo.Target)));
            _toggleModeField.BindProperty(property.Property(nameof(ObjectToggleInfo.ToggleMode)));
            _componentDropdownField.BindProperty(property.Property(nameof(ObjectToggleInfo.Target)));
            
            _menuModeField.BindProperty(menuModeProperty);
            _alternateModeField.BindProperty(altModeProperty);

            OnModeFieldChanged(_alternateModeDisplay, (ToggleMode)_alternateModeDisplay.value);
            
            _paneMenu.SetData(evt =>
            {
                evt.menu.AppendAction("Remove Toggle", _ => onRemoveRequested?.Invoke());
                
                var over = overrideProperty.boolValue;
                var status = over ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                evt.menu.AppendAction("Override Default Value", _ => overrideProperty.SetValue(!over), status);
            });
        }
    }
}