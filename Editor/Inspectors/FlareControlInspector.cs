using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Inspectors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FlareControl))]
    public class FlareControlInspector : FlareInspector
    {
        [PropertyName(nameof(FlareControl.Type))]
        private readonly SerializedProperty _typeProperty = null!;
        
        protected override VisualElement BuildUI(VisualElement root)
        {
            Foldout foldout = new() { text = "Toggle (Puffer)" };
            root.Add(foldout);

            var foldoutToggle = foldout.GetToggle();
            foldoutToggle.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.34f, 0.34f, 0.34f) : new Color(0.705f, 0.705f, 0.705f);
            foldoutToggle.WithPadding(3f).WithRadius(5f);

            foldout.CreatePropertyField(_typeProperty).label = "Control Type";

            
            
            return root;
        }
    }
}