using System;
using System.Collections.Generic;
using System.Linq;
using Flare.Editor.Extensions;
using Flare.Editor.Models;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Elements
{
    public class LayerRuleElement : VisualElement, IFlareBindable
    {
        private readonly PaneMenu _paneMenu;
        private readonly EnumField _causeStateField;
        private readonly EnumField _effectStateField;
        private readonly DropdownField _causeLayerDropdown;
        private readonly DropdownField _effectLayerDropdown;
        
        public LayerRuleElement(IEnumerable<string> layers)
        {
            _paneMenu = new PaneMenu();
            
            _paneMenu.WithWidth(10f).WithHeight(20f);
            _paneMenu.style.marginLeft = 5f;
            
            var topContainer = this.CreateHorizontal();
            topContainer.CreateLabel("Rule").WithFontSize(14f).WithFontStyle(FontStyle.Bold).WithGrow(1f);
            topContainer.Add(_paneMenu);

            Add(topContainer);
            
            var root = this.CreateHorizontal()
                .WithWrap(Wrap.Wrap)
                .WithMaxHeight(192f)
                .WithMinHeight(24f)
                .WithShrink(24f);
            
            // Add text: "When a control from the layer"
            root.CreateLabel("When a control from the layer")
                .WithTextAlign(TextAnchor.MiddleCenter)
                .WithHeight(20f);
            
            // [LAYER]
            _causeLayerDropdown = new DropdownField
            {
                // ReSharper disable once PossibleMultipleEnumeration
                choices = layers.ToList()
            }.WithHeight(20f);
            root.Add(_causeLayerDropdown);

            // Add text: "becomes"
            root.CreateLabel("  becomes")
                .WithTextAlign(TextAnchor.MiddleCenter)
                .WithHeight(20f);
            
            // [ENABLED / DISABLED]
            _causeStateField = new EnumField(ToggleMode.Disabled).WithHeight(20f);
            root.Add(_causeStateField);
            
            root.CreateLabel(" ,")
                .WithTextAlign(TextAnchor.MiddleCenter)
                .WithHeight(20f);
            
            // Add text: "all the controls with the layer"
            root.CreateLabel("all the controls with the layer")
                .WithTextAlign(TextAnchor.MiddleCenter)
                .WithHeight(20f);
            
            // [LAYER]
            _effectLayerDropdown = new DropdownField
            {
                // ReSharper disable once PossibleMultipleEnumeration (we want multiple copies)
                choices = layers.ToList()
            }.WithHeight(20f);
            root.Add(_effectLayerDropdown);
            
            // Add text: "  should be set to"
            root.CreateLabel("  should be set to")
                .WithTextAlign(TextAnchor.MiddleCenter)
                .WithHeight(20f);
            
            // [ENABLED / DISABLED]
            _effectStateField = new EnumField(ToggleMode.Disabled).WithHeight(20f);
            root.Add(_effectStateField);
            
            _causeStateField.RegisterValueChangedCallback(OnCauseStateFieldChange);
            _effectStateField.RegisterValueChangedCallback(OnCauseStateFieldChanged);
        }

        public void SetBinding(SerializedProperty property)
        {
            _causeStateField.Unbind();
            _effectStateField.Unbind();
            _causeLayerDropdown.Unbind();
            _effectLayerDropdown.Unbind();
            
            var causeStateProperty = property.Property(nameof(FlareRule.CauseState));
            var effectStateProperty = property.Property(nameof(FlareRule.EffectState));

            _causeStateField.BindProperty(causeStateProperty);
            _effectStateField.BindProperty(effectStateProperty);
            _causeLayerDropdown.BindProperty(property.Property(nameof(FlareRule.CauseLayer)));
            _effectLayerDropdown.BindProperty(property.Property(nameof(FlareRule.EffectLayer)));
            
            OnModeFieldChanged(_causeStateField, (ToggleMode)causeStateProperty.enumValueIndex);
            OnModeFieldChanged(_effectStateField, (ToggleMode)effectStateProperty.enumValueIndex);
        }

        public void SetData(Action? onRemoveRequested)
        {
            _paneMenu.SetData(evt => evt.menu.AppendAction("Remove", _ => onRemoveRequested?.Invoke()));
        }
        
        private void OnCauseStateFieldChange(ChangeEvent<Enum> evt) => OnModeFieldChanged(_causeStateField, evt);

        private void OnCauseStateFieldChanged(ChangeEvent<Enum> evt) => OnModeFieldChanged(_effectStateField, evt);

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

    }
}