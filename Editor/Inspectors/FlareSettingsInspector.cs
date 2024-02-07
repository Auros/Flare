using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Inspectors
{
    [CustomEditor(typeof(FlareSettings))]
    internal class FlareSettingsInspector : FlareInspector
    {
        [PropertyName(nameof(FlareSettings.WriteDefaults))]
        private readonly SerializedProperty _writeDefaultsProperty = null!;
        
        protected override VisualElement BuildUI(VisualElement root)
        {
            root.CreatePropertyField(_writeDefaultsProperty).WithLabel("Use Write Defaults");
            return root;
        }
    }
}