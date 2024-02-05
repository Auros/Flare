using Flare.Editor.Attributes;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Editor.Views;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Inspectors
{
    [CustomEditor(typeof(FlareControl))]
    public class FlareControlInspector : FlareInspector
    {
        [PropertyName(nameof(FlareControl.Type))]
        private readonly SerializedProperty _typeProperty = null!;
        
        [PropertyName(nameof(FlareControl.Interpolation))]
        private readonly SerializedProperty _interpolationProperty = null!;

        [PropertyName(nameof(FlareControl.MenuItem))]
        private MenuItemControlView? _menuItemControlView;

        [PropertyName(nameof(FlareControl.ObjectToggleCollection))]
        private ObjectToggleCollectionView? _objectToggleCollectionView;
        
        [PropertyName(nameof(FlareControl.PropertyGroupCollection))]
        private PropertyGroupCollectionView? _propertyGroupCollectionView;
        
        [PropertyName(nameof(FlareControl.TagInfo))]
        private TagInfoView? _tagInfoView;

        protected override void OnInitialization()
        {
            if (target is not FlareControl control)
                return;
            
            _objectToggleCollectionView ??= new ObjectToggleCollectionView();
            _propertyGroupCollectionView ??= new PropertyGroupCollectionView();
            _menuItemControlView ??= new MenuItemControlView(control);
            _tagInfoView ??= new TagInfoView();
        }

        protected override VisualElement BuildUI(VisualElement root)
        {
            root.CreatePropertyField(_typeProperty);
            root.CreatePropertyField(_interpolationProperty.Property(nameof(InterpolationInfo.Duration)))
                .WithTooltip("The duration (in seconds) this control takes to execute. A value of \"0\" means instant.");
            
            CategoricalFoldout controlFoldout = new() { text = "Control" };
            _menuItemControlView?.Build(controlFoldout);
            root.Add(controlFoldout);

            CategoricalFoldout toggleFoldout = new() { text = "Object Toggles" };
            _objectToggleCollectionView?.Build(toggleFoldout);
            root.Add(toggleFoldout);

            CategoricalFoldout propertyFoldout = new() { text = "Property Groups" };
            _propertyGroupCollectionView?.Build(propertyFoldout);
            root.Add(propertyFoldout);
            
            CategoricalFoldout tagFoldout = new() { text = "Tags", value = false };
            _tagInfoView?.Build(tagFoldout);
            root.Add(tagFoldout);
            
            return root;
        }
    }
}