using Flare.Editor.Attributes;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    public class LayerInfoView : IView
    {
        private const string _helpText = @"Layers allow you to trigger many controls at once using a simple rule system. When a layer is activated, all controls that are in that layer can be activated/deactivated. Create new layers in the Flare Layer Module";

        [PropertyName(nameof(LayerInfo.Module))]
        private readonly SerializedProperty _moduleProperty = null!;
        
        [PropertyName(nameof(LayerInfo.Layers))]
        private readonly SerializedProperty _layersProperty = null!;
        
        public void Build(VisualElement root)
        {
            HelpBox help = new(_helpText, HelpBoxMessageType.Info);
            root.Add(help);

            var moduleButton = root.CreateButton("Go To Layer Module", () =>
            {
                var control = (_layersProperty.serializedObject.targetObject as FlareControl)!;
                if (!control.LayerInfo.EnsureValidated(control.gameObject))
                    return;
                
                Selection.activeObject = control.LayerInfo.Module;
            });

            root.CreatePropertyField(_layersProperty);
        }
    }
}