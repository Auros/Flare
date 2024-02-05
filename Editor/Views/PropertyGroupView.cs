using Flare.Editor.Attributes;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    internal class PropertyGroupView : IView
    {
        [PropertyName("Array")]
        private readonly SerializedProperty _arrayProperty = null!;
        
        public void Build(VisualElement root)
        {
            FlareCollectionView<PropertyGroupElement> view = new(CreateGroupElement, (e, i) =>
            {
                e.SetData(() =>
                {
                    _arrayProperty.DeleteArrayElementAtIndex(i);
                    _arrayProperty.serializedObject.ApplyModifiedProperties();
                });
            }, _arrayProperty);
            
            root.Add(view);
            root.CreateButton("Add New Group", () =>
            {
                var index = _arrayProperty.arraySize++;
                _arrayProperty.serializedObject.ApplyModifiedProperties();
                _arrayProperty.GetArrayElementAtIndex(index).SetValue(new PropertyGroupInfo()); // Create a new group.
            });
        }

        private static PropertyGroupElement CreateGroupElement()
        {
            PropertyGroupElement root = new();

            root
                .WithBorderColor(FlareUI.BorderColor)
                .WithBorderRadius(3f)
                .WithBorderWidth(1f)
                .WithMarginTop(5f)
                .WithPadding(5f);
            
            return root;
        }
    }
}