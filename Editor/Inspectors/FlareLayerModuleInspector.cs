using System.Collections.Generic;
using System.Linq;
using Flare.Editor.Attributes;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Inspectors
{
    [CustomEditor(typeof(FlareLayerModule))]
    public class FlareLayerModuleInspector : FlareInspector
    {
        [PropertyName(nameof(FlareLayerModule.Rules))]
        private readonly SerializedProperty _rulesProperty = null!;
        
        [PropertyName(nameof(FlareLayerModule.Layers))]
        private readonly SerializedProperty _layersProperty = null!;
        
        protected override VisualElement BuildUI(VisualElement root)
        {
            CategoricalFoldout layersFoldout = new() { text = "Layers" };
            layersFoldout.CreatePropertyField(_layersProperty);
            root.Add(layersFoldout);

            CategoricalFoldout rulesFoldout = new() { text = "Rules" };

            var module = (target as FlareLayerModule)!;

            BuildRulesUI(rulesFoldout, module);
            
            var rulesArrayProperty = _rulesProperty.Field("Array")!;
            rulesFoldout.CreateButton("Add New Rule", () =>
            {
                var index = rulesArrayProperty.arraySize++;
                rulesArrayProperty.serializedObject.ApplyModifiedProperties();
                rulesArrayProperty.GetArrayElementAtIndex(index).SetValue(new FlareRule()); // Create a new flare rule
            });
            
            root.Add(rulesFoldout);

            List<string> previousLayers = new();
            previousLayers.AddRange(module.Layers);
            root.schedule.Execute(() =>
            {
                if (previousLayers.SequenceEqual(module.Layers))
                    return;
                
                previousLayers.Clear();
                previousLayers.AddRange(module.Layers);
                rulesFoldout.Remove(rulesFoldout.Q("RulesList"));
                BuildRulesUI(rulesFoldout, module);

            }).Every(250);
            
            return root;
        }

        private void BuildRulesUI(VisualElement root, FlareLayerModule module)
        {
            var rulesArrayProperty = _rulesProperty.Field("Array")!;
            FlareCollectionView<LayerRuleElement> rules = new(() =>
            {
                LayerRuleElement rule = new(module.Layers);
                rule.WithBackgroundColor(FlareUI.BackgroundColor)
                    .WithBorderColor(FlareUI.BorderColor)
                    .WithBorderRadius(3f)
                    .WithBorderWidth(1f)
                    .WithMarginTop(5f)
                    .WithPadding(5f);

                return rule;
            }, (e, i) =>
            {
                e.SetData(() =>
                {
                    rulesArrayProperty.DeleteArrayElementAtIndex(i);
                    rulesArrayProperty.serializedObject.ApplyModifiedProperties();
                });
            });

            rules.WithName("RulesList");
            rules.SetBinding(rulesArrayProperty);
            root.Add(rules);
            rules.SendToBack();
        }
    }
}