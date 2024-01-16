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

        protected override void OnInitialization()
        {
            if (target is not FlareControl control)
                return;
            
            _menuItemControlView ??= new MenuItemControlView(control);
        }

        protected override VisualElement BuildUI(VisualElement root)
        {
            root.CreatePropertyField(_typeProperty);

            CategoricalFoldout foldout = new() { text = "Control" };
            _menuItemControlView?.Build(foldout);
            
            root.Add(foldout);
            
            return root;
        }
    }
}