using System.Collections.Generic;
using System.Linq;
using Flare.Models;
using nadena.dev.ndmf;
using Sucrose;
using Sucrose.Animation;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Flare.Editor.Passes
{
    internal class TagizationPass : Pass<TagizationPass>
    {
        public override string DisplayName => "Tagization";

        protected override void Execute(BuildContext context)
        {
            var flare = context.GetState<FlareAvatarContext>();
            var sucrose = flare.GetSucrose(context);

            // Create a dummy object because according to Kayla empty animations cause issues with WD OFF
            GameObject dummyObject = new("[Flare] Dummy Object (Ignore)"); // surely no one will name there thing this
            dummyObject.transform.SetParent(context.AvatarRootTransform);
            var emptyAnimation = sucrose.NewAnimation(builder =>
            {
                builder.WithName("[Flare] Dummy Animation for Tag Driver");
                builder.WithBinaryCurve<GameObject>("m_IsActive", 0, 1);
            });
            Object.DestroyImmediate(dummyObject);
            
            Dictionary<string, SucroseParameter> tagDriverParameters = new();
            Dictionary<string, List<ControlContext>> tagControlLookup = new();

            var manager = flare.Tags.FirstOrDefault();
            if (manager == null)
                return;
            
            foreach (var tag in manager.Tags)
            {
                if (tagDriverParameters.ContainsKey(tag))
                    continue;
                
                tagDriverParameters.Add(tag, sucrose.NewParameter()
                    .WithType(SucroseParameterType.Float)
                    .WithName($"[Flare Tags] {tag}")
                );
            }
            
            foreach (var control in flare.ControlContexts)
            {
                foreach (var flareTag in control.Control.TagInfo.Tags)
                {
                    if (!tagControlLookup.TryGetValue(flareTag.Value, out var contexts))
                    {
                        contexts = new List<ControlContext>();
                        tagControlLookup[flareTag.Value] = contexts;
                    }
                    contexts.Add(control);
                }
            }

            var tagDriverLayer = sucrose.NewLayer().WithName("[Flare] Tag Driver");
            var tagDriverDefaultState = tagDriverLayer.NewState()
                .WithName("Default / Reset")
                .WithMotion(emptyAnimation)
                .WithWriteDefaults(flare.PreferredWriteDefaults);

            foreach (var (tag, param) in tagDriverParameters)
            {
                var tagOnState = tagDriverLayer.NewState()
                    .WithName($"{tag} Activator")
                    .WithMotion(emptyAnimation)
                    .WithWriteDefaults(flare.PreferredWriteDefaults);
                
                var tagOffState = tagDriverLayer
                    .NewState()
                    .WithName($"{tag} Deactivator")
                    .WithMotion(emptyAnimation)
                    .WithWriteDefaults(flare.PreferredWriteDefaults);

                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                if (tagControlLookup.TryGetValue(tag, out var controls))
                {
                    foreach (var control in controls)
                    {
                        var id = control.Id;
                        tagDriverDefaultState.TransitionTo(tagOnState)
                            .WithCondition(id, AnimatorConditionMode.Greater, 0.5f)
                            .WithCondition(param, AnimatorConditionMode.Less, 0.5f);
                    
                        tagDriverDefaultState.TransitionTo(tagOffState)
                            .WithCondition(id, AnimatorConditionMode.Less, 0.5f)
                            .WithCondition(param, AnimatorConditionMode.Greater, 0.5f);
                    }
                }

                var onStateParamDriver = tagOnState.GetStateMachineBehaviour<VRCAvatarParameterDriver>();
                onStateParamDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    name = param.Name,
                    value = 1
                });

                var offStateParamDriver = tagOffState.GetStateMachineBehaviour<VRCAvatarParameterDriver>();
                offStateParamDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    name = param.Name,
                    value = 0
                });
                
                tagOnState.TransitionTo(tagDriverDefaultState).WithCondition(param, AnimatorConditionMode.Greater, 0.5f);
                tagOffState.TransitionTo(tagDriverDefaultState).WithCondition(param, AnimatorConditionMode.Less, 0.5f);

                var rules = manager.Rules.Where(r => r.CauseLayer == tag);
                foreach (var rule in rules)
                {
                    var effectLayer = rule.EffectLayer;
                    if (!tagControlLookup.TryGetValue(effectLayer, out var effectControls))
                        continue;
                    
                    var targetDriver = rule.CauseState is ToggleMode.Enabled ? onStateParamDriver : offStateParamDriver;
                    var targetValue = rule.EffectState is ToggleMode.Enabled ? 1f : 0f;

                    foreach (var effectControl in effectControls)
                    {
                        targetDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                        {
                            type = VRC_AvatarParameterDriver.ChangeType.Set,
                            name = effectControl.Id,
                            value = targetValue
                        });
                    }
                }
            }
        }
    }
}