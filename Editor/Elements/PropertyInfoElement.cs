using System;
using System.IO;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Extensions;
using Flare.Editor.Models;
using Flare.Editor.Windows;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Flare.Editor.Elements
{
    internal class PropertyInfoElement : VisualElement, IFlareBindable
    {
        private readonly Label _nameLabel;
        private readonly PaneMenu _paneMenu;
        private readonly Button _selectButton;
        private readonly ObjectField _referenceField;
        private readonly CombinatoryFieldElement _valuePreview;
        private readonly CombinatoryFieldElement _valueSelector;
        private readonly CombinatoryFieldElement _overrideValueSelector;
        private readonly EnumField _controlStateDropdown;

        private Action? _selector;
        private SerializedProperty? _target;
        
        public PropertyInfoElement()
        {
            _nameLabel = new Label();
            _paneMenu = new PaneMenu();
            _valuePreview = new CombinatoryFieldElement();
            _valueSelector = new CombinatoryFieldElement();
            _overrideValueSelector = new CombinatoryFieldElement();
            _controlStateDropdown = new EnumField();
            _referenceField = new ObjectField();
            
            var topContainer = this.CreateHorizontal().WithMargin(4f);
            topContainer.Add(_nameLabel);
            topContainer.CreateElement().WithGrow(1f);
            topContainer.Add(_paneMenu);
            
            _valuePreview.Enabled(false);
            _referenceField.Enabled(false);
            _valuePreview.SetLabel("Current Value");
            _valueSelector.SetLabel("Target Value");
            _overrideValueSelector.SetLabel("Default Value");
            _controlStateDropdown.label = "Control State";
            _referenceField.AddToClassList(ObjectField.alignedFieldUssClassName);
            _controlStateDropdown.AddToClassList(ObjectField.alignedFieldUssClassName);
            _controlStateDropdown.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null)
                    return;
                
                OnStateFieldChanged(_controlStateDropdown, (ControlState)evt.newValue);
            });
            _referenceField.label = "Reference Object";
            
            _paneMenu.style.marginLeft = 5f;
            _paneMenu.WithWidth(8f).WithHeight(16f);
            _nameLabel.WithFontStyle(FontStyle.Bold).WithFontSize(13f);
            
            Add(topContainer);
            Add(_referenceField);
            Add(_valuePreview);
            Add(_overrideValueSelector);
            Add(_valueSelector);
            Add(_controlStateDropdown);
            
            _selectButton = this.CreateButton("Select Property");
        }
        
        public void SetData(Action? onRemoveRequested = null)
        {
            _paneMenu.SetData(evt =>
            {
                evt.menu.AppendAction("Remove", _ => onRemoveRequested?.Invoke());

                var nameProperty = _target?.Property(nameof(PropertyInfo.Name));
                var overrideProperty = _target?.Property(nameof(PropertyInfo.OverrideDefaultValue));
                
                var targetAvailable = _target != null && !string.IsNullOrEmpty(nameProperty?.stringValue);
                evt.menu.AppendAction("Change Property", _ => ShowPropertyWindow(_target), targetAvailable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                if (overrideProperty is null)
                    return;
                
                var overrideValue = overrideProperty.boolValue;
                var status = overrideValue ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                var newValue = !overrideValue;
                
                evt.menu.AppendAction("Override Default Value", _ => overrideProperty.SetValue(newValue), status);
            });
        }

        public void SetBinding(SerializedProperty property)
        {
            // We need to copy this property for the Select Property callback.
            // Otherwise, it could (will) be iterated over.
            property = property.Copy();
            _nameLabel.Unbind();
            _selectButton.Unbind();
            _controlStateDropdown.Unbind();
            _overrideValueSelector.Unbind();

            var nameProperty = property.Property(nameof(PropertyInfo.Name));
            var pathProperty = property.Property(nameof(PropertyInfo.Path));
            var stateProperty = property.Property(nameof(PropertyInfo.State));
            var overrideProperty = property.Property(nameof(PropertyInfo.OverrideDefaultValue));
            var contextTypeProperty = property.Property(nameof(PropertyInfo.ContextType));
            var contextType = Type.GetType(contextTypeProperty.stringValue);

            _nameLabel.TrackPropertyValue(property, prop =>
            {
                var propertyName = prop.Property(nameof(PropertyInfo.Name)).stringValue;
                var propertyPath = prop.Property(nameof(PropertyInfo.Path)).stringValue;
                
                contextType = Type.GetType(contextTypeProperty.stringValue);
                _referenceField.value = GetPropertyReference(propertyPath, contextType);
                _referenceField.Visible(!string.IsNullOrWhiteSpace(propertyName));
                _nameLabel.text = GetDisplayName(propertyName);
                _nameLabel.tooltip = propertyPath;
            });
            
            _selectButton.TrackPropertyValue(nameProperty, prop =>
            {
                _selectButton.Visible(string.IsNullOrEmpty(prop.stringValue));
            });
            _controlStateDropdown.TrackPropertyValue(nameProperty, prop =>
            {
                var propertyNameExists = string.IsNullOrEmpty(prop.stringValue);
                _controlStateDropdown.Visible(!propertyNameExists);
            });
            _controlStateDropdown.TrackPropertyValue(stateProperty, prop =>
            {
                var isEnabled = (ControlState)prop.enumValueIndex is ControlState.Enabled;
                var whileText = !isEnabled ? "Active" : "Inactive";
                _overrideValueSelector.SetLabel($"Default Value (While {whileText})");
            });
            
            var isEnabled = (ControlState)stateProperty.enumValueIndex is ControlState.Enabled;
            var whileText = !isEnabled ? "Active" : "Inactive";
            _overrideValueSelector.SetLabel($"Default Value (While {whileText})");
            
            _overrideValueSelector.SetMode(true);
            _overrideValueSelector.Visible(overrideProperty.boolValue);
            _nameLabel.TrackPropertyValue(overrideProperty, prop =>
            {
                _overrideValueSelector.Visible(prop.boolValue);
            });
            
            _controlStateDropdown.BindProperty(stateProperty);
            
            var propertyName = nameProperty.stringValue;
            _nameLabel.text = GetDisplayName(propertyName);
            _nameLabel.tooltip = pathProperty.stringValue;
            _selectButton.Visible(string.IsNullOrEmpty(propertyName));
            _controlStateDropdown.Visible(!string.IsNullOrEmpty(propertyName));
            _referenceField.Visible(!string.IsNullOrWhiteSpace(propertyName));
            _referenceField.value = GetPropertyReference(pathProperty.stringValue, contextType);
            
            _selectButton.clicked -= _selector;
            _selector = () => ShowPropertyWindow(property);
            _selectButton.clicked += _selector;
            _target = property;
            
            _valuePreview.SetBinding(_target);
            _valueSelector.SetBinding(_target);
            _overrideValueSelector.SetBinding(_target);
            OnStateFieldChanged(_controlStateDropdown, (ControlState)stateProperty.enumValueIndex);
        }
        
        // ReSharper disable once SuggestBaseTypeForParameter
        private static void OnStateFieldChanged(EnumField field, ControlState value)
        {
            // Color the enum dropdown text according to the value of the toggle mode.
            var enabledColor = FlareUI.EnabledColor;
            var disabledColor = FlareUI.DisabledColor;
            
            var targetColor = value is ControlState.Enabled ? enabledColor : disabledColor;
            
            field.Query<TextElement>().Last().WithColor(targetColor);
        }

        private static string GetDisplayName(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? "(No Property Selected)" : input;
        }
        
        private static void ShowPropertyWindow(SerializedProperty? property)
        {
            if (property is null)
                return;
            
            PropertySelectorWindow.Present(property);
        }

        private static Object? GetPropertyReference(string path, Type? context)
        {
            if (context == null)
                return null;
            
            
            var go = GameObject.Find(path);
            if (context == typeof(GameObject) && go)
                return go!;
            
            return go == null ? null : go.GetComponent(context);
        }
    }
}