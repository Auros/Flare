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
            
            if (flare.IsEmpty)
                return;
            
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
                .WithName("Default")
                .WithMotion(emptyAnimation)
                .WithWriteDefaults(flare.PreferredWriteDefaults);

            SucroseParameter? triggeringParameter = null;
            List<SucroseParameter> everyTriggerParameter = new();

            // Wow, this feels wrong to write
            foreach (var tag in manager.Tags)
            {
                foreach (var triggerParameter in flare.GetTriggerParameters(tag))
                {
                    var control = flare.GetTriggerContext(triggerParameter.Name);
                    if (control is null)
                        return;

                    var setTo = control.Control.MenuItem.TriggerMode is ToggleMode.Enabled ? 1 : 0;
                    
                    var triggerOnState = tagDriverLayer.NewState()
                        .WithName($"{control.Id} Trigger")
                        .WithMotion(emptyAnimation)
                        .WithWriteDefaults(flare.PreferredWriteDefaults);
                    
                    var triggerOnParamDriver = triggerOnState.GetStateMachineBehaviour<VRCAvatarParameterDriver>();
                    
                    if (tagControlLookup.TryGetValue(tag, out var controls))
                    {
                        foreach (var tagControl in controls)
                        {
                            triggerOnParamDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                            {
                                type = VRC_AvatarParameterDriver.ChangeType.Set,
                                name = tagControl.Id,
                                value = setTo
                            });
                        }
                    }

                    triggeringParameter ??= sucrose.NewParameter()
                        .WithType(SucroseParameterType.Float)
                        .WithName("[Flare Tags] Triggering");
                    
                    triggerOnParamDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Set,
                        name = triggeringParameter.Name,
                        value = 1f
                    });
                    
                    everyTriggerParameter.Add(triggerParameter);

                    tagDriverDefaultState.TransitionTo(triggerOnState)
                        .WithCondition(triggerParameter, AnimatorConditionMode.Greater, 0.5f)
                        .Destination.Exit(); // Exit animator
                }
            }
            
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

            if (everyTriggerParameter.Count is 0 || triggeringParameter is null)
                return;

            var resetState = tagDriverLayer.NewState()
                .WithName("Trigger Reset")
                .WithMotion(emptyAnimation)
                .WithWriteDefaults(flare.PreferredWriteDefaults);

            var resetDriver = resetState.GetStateMachineBehaviour<VRCAvatarParameterDriver>();
            resetDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                name = triggeringParameter.Name,
                value = 0
            });
            
            var transition = tagDriverDefaultState.TransitionTo(resetState);
            transition.WithCondition(triggeringParameter, AnimatorConditionMode.Greater, 0.5f);
            
            // Wait for all triggers to stop before resetting
            foreach (var triggerParam in everyTriggerParameter)
                transition.WithCondition(triggerParam, AnimatorConditionMode.Less, 0.5f);

            transition.Destination.Exit();
        }
    }
}