using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Flare.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(PropertyGroupInfo))]
    public class PropertyGroupInfoPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            
            var selectionTypeProperty = property.Property(nameof(PropertyGroupInfo.SelectionType));
            var selectionField = root.CreatePropertyField(selectionTypeProperty).WithLabel("Selection Mode");
            selectionField.RegisterValueChangeCallback(SubscribeToInclusionFields);

            // Setup Inclusion and Exclusion fields.
            var inclusionsProperty = property.Property(nameof(PropertyGroupInfo.Inclusions));
            var includesField = root.CreatePropertyField(inclusionsProperty)
                .WithLabel("Target Objects").WithName(nameof(PropertyGroupInfo.Inclusions));
            
            var exclusionsProperty = property.Property(nameof(PropertyGroupInfo.Exclusions));
            var excludesField = root.CreatePropertyField(exclusionsProperty)
                .WithLabel("Objects To Exclude").WithName(nameof(PropertyGroupInfo.Exclusions));
            
            var selectionType = (PropertySelectionType)selectionTypeProperty.enumValueIndex;
            ToggleInclusionFields(selectionType, includesField, excludesField);
            
            // Setup Property Management *gulp*
            var propertiesProperty = property.Property(nameof(PropertyGroupInfo.Properties));
            var properties = root.CreatePropertyField(propertiesProperty);
            root.style.marginBottom = 30;
            
            return root;
        }

        private static void SubscribeToInclusionFields(SerializedPropertyChangeEvent ctx)
        {
            if (ctx.target is not VisualElement element)
                return;
            
            var selectionType = (PropertySelectionType)ctx.changedProperty!.enumValueIndex;

            var includesField = element.parent.Q<VisualElement>(nameof(PropertyGroupInfo.Inclusions));
            var excludesField = element.parent.Q<VisualElement>(nameof(PropertyGroupInfo.Exclusions));
            
            ToggleInclusionFields(selectionType, includesField, excludesField);
        }

        private static void ToggleInclusionFields(PropertySelectionType type, VisualElement inclusionField, VisualElement exclusionField)
        {
            inclusionField.style.display = type is PropertySelectionType.Normal ? DisplayStyle.Flex : DisplayStyle.None;
            exclusionField.style.display = type is PropertySelectionType.Normal ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}