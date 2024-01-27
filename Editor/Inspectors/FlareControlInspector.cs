using Flare.Editor.Attributes;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Editor.Views;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Inspectors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FlareControl))]
    public class FlareControlInspector : FlareInspector
    {
        [PropertyName(nameof(FlareControl.Type))]
        private readonly SerializedProperty _typeProperty = null!;

        [PropertyName(nameof(FlareControl.MenuItem))]
        private MenuItemControlView? _menuItemControlView;

        [PropertyName(nameof(FlareControl.ObjectToggleCollection))]
        private ObjectToggleView? _objectToggleView;
        
        [PropertyName(nameof(FlareControl.PropertyGroupCollection))]
        private PropertyGroupView? _propertyGroupView;

        protected override void OnInitialization()
        {
            if (target is not FlareControl control)
                return;
            
            _objectToggleView ??= new ObjectToggleView();
            _propertyGroupView ??= new PropertyGroupView();
            _menuItemControlView ??= new MenuItemControlView(control);
        }

        protected override VisualElement BuildUI(VisualElement root)
        {
            root.CreatePropertyField(_typeProperty);

            CategoricalFoldout controlFoldout = new() { text = "Control" };
            _menuItemControlView?.Build(controlFoldout);
            root.Add(controlFoldout);

            CategoricalFoldout toggleFoldout = new() { text = "Object Toggles" };
            _objectToggleView?.Build(toggleFoldout);
            root.Add(toggleFoldout);

            CategoricalFoldout propertyFoldout = new() { text = "Property Groups" };
            _propertyGroupView?.Build(propertyFoldout);
            root.Add(propertyFoldout);
            
            return root;
        }
    }
}