using System.Linq;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;
using VRC.SDKBase;

namespace Flare.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(FlareTag))]
    internal class FlareTagPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            var flareControl = property.serializedObject.targetObject as FlareControl;
            if (flareControl == null)
            {
                root.CreateLabel("FlareTag property not located on FlareControl").WithPadding(3f);
                return root;
            }
            
            var descriptor = flareControl.GetComponentInParent<VRC_AvatarDescriptor>();
            if (descriptor == null)
            {
                root.CreateLabel("Cannot find Avatar Descriptor");
                return root;
            }

            var layerInfo = flareControl.TagInfo;
            
            if (!layerInfo.EnsureValidated(flareControl.gameObject))
            {
                root.CreateLabel("Cannot find Tag Module");
                return root;
            }

            var module = descriptor.GetComponentInChildren<FlareTags>();

            if (module!.Tags.Length is 0)
            {
                root.CreateLabel("No labels exist! Create some in the Tag Module").WithPadding(3f);
                return root;
            }

            DropdownField dropdown = new()
            {
                index = 0,
                label = "Tag",
                choices = module.Tags.Prepend("Select Tag").ToList()
            };

            var valueProperty = property.Property(nameof(FlareTag.Value));
            dropdown.RegisterValueChangedCallback(evt =>
            {
                valueProperty.SetValue(dropdown.index is 0 ? string.Empty : evt.newValue);
            });

            var layer = valueProperty.stringValue;
            var targetIndex = layer != string.Empty ? module.Tags.ToList().IndexOf(valueProperty.stringValue) : -1;
            dropdown.index = targetIndex is -1 ? 0 : ++targetIndex;
            
            root.Add(dropdown);

            return root;
        }
    }
}