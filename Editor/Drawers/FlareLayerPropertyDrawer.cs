using System.Linq;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Extensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDKBase;
using FlareLayer = Flare.Models.FlareLayer;

namespace Flare.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(FlareLayer))]
    public class FlareLayerPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            var flareControl = property.serializedObject.targetObject as FlareControl;
            if (flareControl == null)
            {
                root.CreateLabel("FlareLayer proeprty not located on FlareControl").WithPadding(3f);
                return root;
            }
            
            var descriptor = flareControl.GetComponentInParent<VRC_AvatarDescriptor>();
            if (descriptor == null)
            {
                root.CreateLabel("Cannot find Avatar Descriptor");
                return root;
            }

            var layerInfo = flareControl.LayerInfo;
            
            if (!layerInfo.EnsureValidated(flareControl.gameObject))
            {
                root.CreateLabel("Cannot find Layer Module");
                return root;
            }

            var module = flareControl.LayerInfo.Module;

            if (module!.Layers.Length is 0)
            {
                root.CreateLabel("No labels exist! Create some in the Layer Module").WithPadding(3f);
                return root;
            }

            DropdownField dropdown = new()
            {
                index = 0,
                label = "Layer",
                choices = module.Layers.Prepend("Select Layer").ToList()
            };

            var valueProperty = property.Property(nameof(FlareLayer.Value));
            dropdown.RegisterValueChangedCallback(evt =>
            {
                valueProperty.SetValue(dropdown.index is 0 ? string.Empty : evt.newValue);
            });

            var layer = valueProperty.stringValue;
            var targetIndex = layer != string.Empty ? module.Layers.ToList().IndexOf(valueProperty.stringValue) : -1;
            dropdown.index = targetIndex is -1 ? 0 : ++targetIndex;
            
            root.Add(dropdown);

            return root;
        }
    }
}