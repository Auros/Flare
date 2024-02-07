using System;
using System.Collections.Generic;
using Flare.Models;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using Sucrose;
using Sucrose.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Flare.Editor
{
    [PublicAPI]
    internal class FlareAvatarContext
    {
        private SucroseContainer? _container;
        private SucroseBlendTree? _directBlendTree;
        private SucroseParameter? _weightParameter;
        private SucroseBlendTree? _smoothingBlendTree;
        private SucroseParameter? _frameTimeParameter;
        private readonly List<FlareTags> _tags = new();
        private readonly List<FlareControl> _controls = new();
        private readonly List<ControlContext> _controlContexts = new();
        private readonly Dictionary<string, ControlContext> _triggerControls = new();
        private readonly Dictionary<string, List<SucroseParameter>> _triggerParameters = new();

        public IReadOnlyList<FlareTags> Tags => _tags;
        
        public IReadOnlyList<FlareControl> Controls => _controls;

        public IReadOnlyList<ControlContext> ControlContexts => _controlContexts;

        public bool PreferredWriteDefaults { get; internal set; }
        
        public void AddTags(FlareTags tags)
        {
            _tags.Add(tags);
        }
        
        public void AddControl(FlareControl control)
        {
            _controls.Add(control);
        }
        
        public void AddControlContext(ControlContext controlContext)
        {
            var control = controlContext.Control;
            _controlContexts.Add(controlContext);
            
            if (control.Type is ControlType.Menu && control.MenuItem.Type is MenuItemType.Button
                && control.MenuItem.IsTagTrigger && !string.IsNullOrWhiteSpace(control.MenuItem.Tag))
            {
                _triggerControls[controlContext.Id] = controlContext;
            }
        }

        public ControlContext? GetTriggerContext(string id)
        {
            return !_triggerControls.ContainsKey(id) ? null : _triggerControls[id];
        }
        
        public IReadOnlyList<SucroseParameter> GetTriggerParameters(string tag)
        {
            if (!_triggerParameters.TryGetValue(tag, out var list))
                return Array.Empty<SucroseParameter>();
            
            return list;
        }

        public void AddTrigger(string tag, SucroseParameter parameter)
        {
            if (!_triggerParameters.TryGetValue(tag, out var list))
            {
                list = new List<SucroseParameter>();
                _triggerParameters[tag] = list;
            }
            list.Add(parameter);
        }

        public SucroseContainer GetSucrose(BuildContext build)
        {
            if (_container is not null)
                return _container;
            
            VRCAvatarDescriptor.CustomAnimLayer? fx = null;
            var layers = build.AvatarDescriptor.baseAnimationLayers;
            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                if (layer.type is not VRCAvatarDescriptor.AnimLayerType.FX)
                    continue;

                if (!layer.animatorController)
                {
                    var controller = new AnimatorController
                    {
                        name = "[Flare] FX"
                    };
                    AssetDatabase.AddObjectToAsset(controller, build.AssetContainer);
                    layers[i].animatorController = controller;
                }

                layers[i].isDefault = false;
                fx = layers[i];
                break;
            }

            if (fx is null)
                throw new InvalidOperationException("Cannot find FX layer");
            
            _container = new SucroseContainer((fx.Value.animatorController as AnimatorController)!);
            
            if (_container.LayerCount is 0)
                _container.NewLayer().WithName("Base Layer").WithWeight(1f);
            
            return _container;
        }

        public SucroseParameter GetFrameTimeParameter(BuildContext build)
        {
            if (_frameTimeParameter is not null)
                return _frameTimeParameter;

            var sucrose = GetSucrose(build);
            var weightParameter = GetWeightParameter(build);

            var timeParameter = sucrose.NewParameter()
                .WithType(SucroseParameterType.Float)
                .WithName("[Flare Internal] Time");
            
            var frameTimeParameter = sucrose.NewParameter()
                .WithType(SucroseParameterType.Float)
                .WithName("[Flare Internal] Frame Time");
            
            var lastTimeParameter = sucrose.NewParameter()
                .WithType(SucroseParameterType.Float)
                .WithName("[Flare Internal] Last Time");

            sucrose.NewLayer()
                .WithName("[Flare] Frame Time Layer")
                .NewState()
                .WithName("Time")
                .WithMotion(clip =>
                {
                    clip.WithWrapMode(WrapMode.Loop);
                    clip.WithName("Time (0 to 20,000)");
                    clip.WithCurve(string.Empty, typeof(Animator), timeParameter.Name, curve =>
                    {
                        curve.AddKeyframe(0, 0f);
                        curve.AddKeyframe(20_000f, 20_000f);
                    });
                });

            sucrose.NewLayer()
                .WithName("[Flare] Frame Logic Layer")
                .NewBlendTree()
                    .WithType(BlendTreeType.Direct)
                    .WithName("Frame Time Measurer")
                    .WithParameter(weightParameter)
                .NewChildMotion()
                    .WithDirectParameter(timeParameter)
                    .WithMotion(clip =>
                    {
                        clip.WithName("[Flare] Frame Time (1)");
                        clip.WithBinaryCurve<Animator>(frameTimeParameter.Name, 1f);
                    })
                    .BlendTree
                .NewChildMotion()
                    .WithDirectParameter(lastTimeParameter)
                    .WithMotion(clip =>
                    {
                        clip.WithName("[Flare] Frame Time (-1)");
                        clip.WithBinaryCurve<Animator>(frameTimeParameter.Name, -1f);
                    })
                    .BlendTree
                .NewChildMotion()
                    .WithDirectParameter(timeParameter)
                    .WithMotion(clip =>
                    {
                        clip.WithName("[Flare] Last Time (1)");
                        clip.WithBinaryCurve<Animator>(lastTimeParameter.Name, 1f);
                    });

            _frameTimeParameter = frameTimeParameter;
            return _frameTimeParameter;
        }

        public SucroseBlendTree GetDirectBlendTree(BuildContext build)
        {
            if (_directBlendTree is not null)
                return _directBlendTree;
            
            var sucrose = GetSucrose(build);
            var weightParameter = GetWeightParameter(build);

            _directBlendTree = sucrose.NewLayer()
                .WithName("[Flare] Primary DBT Layer")
                .NewBlendTree()
                    .WithType(BlendTreeType.Direct)
                    .WithName("[Flare] Primary DBT")
                    .WithParameter(weightParameter);
            
            return _directBlendTree;
        }
        
        
        public SucroseBlendTree GetSmoothingBlendTree(BuildContext build)
        {
            if (_smoothingBlendTree is not null)
                return _smoothingBlendTree;
            
            var sucrose = GetSucrose(build);
            var weightParameter = GetWeightParameter(build);

            _smoothingBlendTree = sucrose.NewLayer()
                .WithName("[Flare] Smoothing DBT Layer")
                .NewBlendTree()
                    .WithType(BlendTreeType.Direct)
                    .WithName("[Flare] Smoothing DBT")
                    .WithParameter(weightParameter);
            
            return _smoothingBlendTree;
        }

        public SucroseParameter GetWeightParameter(BuildContext context)
        {
            return _weightParameter ??= GetSucrose(context)
                .NewParameter()
                    .WithType(SucroseParameterType.Float)
                    .WithName("[Flare Internal] Weight")
                    .WithDefaultValue(1f);
        }
    }
}