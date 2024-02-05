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
    [CustomEditor(typeof(FlareTags))]
    internal class FlareTagModuleInspector : FlareInspector
    {
        [PropertyName(nameof(FlareTags.Rules))]
        private readonly SerializedProperty _rulesProperty = null!;
        
        [PropertyName(nameof(FlareTags.Tags))]
        private readonly SerializedProperty _layersProperty = null!;
        
        protected override VisualElement BuildUI(VisualElement root)
        {
            CategoricalFoldout layersFoldout = new() { text = "Tags" };
            layersFoldout.CreatePropertyField(_layersProperty).WithName("Tags");
            root.Add(layersFoldout);

            CategoricalFoldout rulesFoldout = new() { text = "Rules" };

            var module = (target as FlareTags)!;

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
            previousLayers.AddRange(module.Tags);
            root.schedule.Execute(() =>
            {
                if (previousLayers.SequenceEqual(module.Tags))
                    return;
                
                previousLayers.Clear();
                previousLayers.AddRange(module.Tags);
                rulesFoldout.Remove(rulesFoldout.Q("RulesList"));
                BuildRulesUI(rulesFoldout, module);

            }).Every(250);
            
            return root;
        }

        private void BuildRulesUI(VisualElement root, FlareTags module)
        {
            var rulesArrayProperty = _rulesProperty.Field("Array")!;
            FlareCollectionView<TagRuleElement> rules = new(() =>
            {
                TagRuleElement rule = new(module.Tags);
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