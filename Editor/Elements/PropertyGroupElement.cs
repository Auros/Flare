using System;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Extensions;
using Flare.Editor.Models;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Flare.Editor.Elements
{
    internal class PropertyGroupElement : VisualElement, IFlareBindable
    {
        private readonly PaneMenu _paneMenu;
        private readonly PropertyField _selectionField;
        private readonly PropertyField _inclusionField;
        private readonly PropertyField _exclusionField;
        private readonly FlareCollectionView<PropertyInfoElement> _propertyCollectionView;
        private SerializedProperty? _propertiesProperty;
        
        public PropertyGroupElement()
        {
            _paneMenu = new PaneMenu();
            _selectionField = new PropertyField();
            _inclusionField = new PropertyField();
            _exclusionField = new PropertyField();
            _propertyCollectionView = new FlareCollectionView<PropertyInfoElement>(CreatePropertyInfoElement);
            
            _selectionField.label = "Selection Mode";
            _inclusionField.WithLabel("Target Objects");
            _exclusionField.WithLabel("Objects To Exclude");
            _selectionField.RegisterValueChangeCallback(UpdateSelectorTargetField);

            _paneMenu.style.marginLeft = 5f;
            _inclusionField.style.marginLeft = 15f;
            _exclusionField.style.marginLeft = 15f;
            _paneMenu.WithWidth(10f).WithHeight(20f);
            
            // Create the top shelf with the object picker and context menu.
            var topContainer = this.CreateHorizontal();
            topContainer.CreateLabel("<b>Configure</b>").WithGrow(1f).WithFontSize(14f).WithPadding(3f);
            topContainer.Add(_paneMenu);
            
            Add(_selectionField);
            Add(_inclusionField);
            Add(_exclusionField);

            this.CreateLabel("<b>Properties</b>").WithFontSize(14f).WithPadding(3f);

            var propertyViewContainer = this.CreateElement();
            propertyViewContainer.style.marginLeft = 15f;
            propertyViewContainer.style.marginRight = 15f;
            propertyViewContainer.Add(_propertyCollectionView);

            propertyViewContainer.CreateHorizontalSpacer(5f);
            propertyViewContainer.CreateButton("New Property", () =>
            {
                if (_propertiesProperty is null)
                    return;

                var index = _propertiesProperty.arraySize++;
                _propertiesProperty.serializedObject.ApplyModifiedProperties();
                _propertiesProperty.GetArrayElementAtIndex(index).SetValue(new PropertyInfo()); // Create new prop info.
            });
            
            UpdateSelectorTargetField(PropertySelectionType.Normal);
        }

        private void UpdateSelectorTargetField(SerializedPropertyChangeEvent evt)
        {
            var type = (PropertySelectionType)evt.changedProperty.enumValueIndex;
            UpdateSelectorTargetField(type);
        }

        private void UpdateSelectorTargetField(PropertySelectionType type)
        {
            _inclusionField.Visible(type is PropertySelectionType.Normal);
            _exclusionField.Visible(type is PropertySelectionType.Avatar);
        }

        public void SetData(Action? onRemoveRequested = null)
        {
            _paneMenu.SetData(evt => evt.menu.AppendAction("Remove", _ => onRemoveRequested?.Invoke()));
        }

        public void SetBinding(SerializedProperty property)
        {
            _selectionField.Unbind();
            _inclusionField.Unbind();
            _exclusionField.Unbind();

            _selectionField.BindProperty(property.Property(nameof(PropertyGroupInfo.SelectionType)));
            _inclusionField.BindProperty(property.Property(nameof(PropertyGroupInfo.Inclusions)));
            _exclusionField.BindProperty(property.Property(nameof(PropertyGroupInfo.Exclusions)));

            var propertiesProperty = property.Property(nameof(PropertyGroupInfo.Properties));
            var arrayProperty = propertiesProperty.Field("Array")!;
            _propertiesProperty = propertiesProperty;

            _propertyCollectionView.SetData(OnPropertyInfoBind);
            _propertyCollectionView.SetBinding(arrayProperty);
        }
        
        
        private void OnPropertyInfoBind(PropertyInfoElement element, int index)
        {
            if (_propertiesProperty is null)
                return;

            var property = _propertiesProperty.GetArrayElementAtIndex(index);
            element.SetBinding(property);
            element.SetData(() =>
            {
                _propertiesProperty.DeleteArrayElementAtIndex(index);
                _propertiesProperty.serializedObject.ApplyModifiedProperties();
            });
        }

        private static PropertyInfoElement CreatePropertyInfoElement()
        {
            PropertyInfoElement root = new();
            
            root.WithBackgroundColor(FlareUI.BackgroundColor)
                .WithBorderColor(FlareUI.BorderColor)
                .WithBorderRadius(3f)
                .WithBorderWidth(1f)
                .WithMarginTop(5f)
                .WithPadding(5f);
            
            return root;
        }

    }
}