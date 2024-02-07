using System;
using System.Linq;
using Flare.Editor.Models;
using Flare.Editor.Services;
using Flare.Models;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Flare.Editor.Passes
{
    /// <summary>
    /// Converts control data into corresponding control containers.
    /// </summary>
    internal class ContainerizationPass : Pass<ContainerizationPass>
    {
        public override string DisplayName => "Containerization";

        protected override void Execute(BuildContext context)
        {
            var avatarRoot = context.AvatarRootTransform;
            var flare = context.GetState<FlareAvatarContext>();

            foreach (var control in flare.Controls)
            {
                var id = GetId(control, flare);
                ControlContext controlContext = new(id, control);
                flare.AddControlContext(controlContext);
                
                foreach (var toggle in control.ObjectToggleCollection.Toggles)
                {
                    // Ignore non-toggleable properties.
                    if (toggle.Target is null)
                        continue;
                    
                    var name = "m_IsActive";
                    var type = typeof(GameObject);
                    var (path, defaultValue) = toggle.Target switch
                    {
                        GameObject go => (go.transform.GetAnimatablePath(avatarRoot), go.activeSelf),
                        Renderer renderer => (renderer.transform.GetAnimatablePath(avatarRoot), renderer.enabled),
                        Behaviour behaviour => (behaviour.transform.GetAnimatablePath(avatarRoot), behaviour.enabled),
                        _ => (null, false)
                    };

                    if (path is null)
                    {
                        // TODO: Report insigificant error.
                        // NDMF has an error report API but there's barely any docs on how to use it properly.
                        continue;
                    }

                    float defaultFloatValue;
                    float inverseFloatValue;
                    if (toggle.MenuMode is ToggleMenuState.Active)
                    {
                        defaultFloatValue = defaultValue ? 1f : 0f;
                        inverseFloatValue = toggle.ToggleMode is ToggleMode.Enabled ? 1f : 0f;
                    }
                    else
                    {
                        defaultFloatValue = toggle.ToggleMode is ToggleMode.Enabled ? 1f : 0f;
                        inverseFloatValue = defaultValue ? 1f : 0f;
                    }
                    
                    if (toggle.Target is Renderer or Behaviour)
                    {
                        name = "m_Enabled";
                        type = toggle.Target.GetType();
                    }
                    
                    AnimatableBinaryProperty property = new(type, path, name, defaultFloatValue, inverseFloatValue);
                    controlContext.AddProperty(property);
                }

                foreach (var group in control.PropertyGroupCollection.Groups)
                {
                    var root = context.AvatarRootObject;
                    
                    // ReSharper disable once ConvertIfStatementToSwitchStatement
                    if (group.SelectionType is PropertySelectionType.Normal)
                    {
                        // This could be more optimized, but for now I'll leave it like this for simplicity (I'm tired.)
                        var include = group.Inclusions;
                        BindingService binder = new(root, include);
                        foreach (var prop in group.Properties)
                        {
                            BindAnimatablePropertiesFast(controlContext, binder, prop);
                        }

                    }
                    else if (group.SelectionType is PropertySelectionType.Avatar)
                    {
                        var exclude = group.Exclusions;
                        
                        BindingService binder = new(root, exclude, typeof(Renderer), typeof(VRCPhysBone));
                        var bindings = binder.GetPropertyBindings(group.Properties, false)
                            .Where(b => b.GameObject != root)
                            .ToLookup(k => k.Name);

                        foreach (var prop in group.Properties)
                        {
                            foreach (var animatable in bindings[prop.Name])
                            {
                                // Skip over properties with mismatching types.
                                if (animatable.Type != prop.ValueType)
                                    continue;
                                
                                BindPropertyToAnimatable(controlContext, binder, prop, animatable);
                            }
                        }
                    }
                }
            }
        }

        // I needed this to be faster so I couldn't directly use the BindingService to get the properties.
        private static void BindAnimatablePropertiesFast(ControlContext controlContext, BindingService binder, PropertyInfo prop)
        {
            var type = Type.GetType(prop.ContextType);
            if (type == null)
            {
                // MAYBE: Warn?
                return;
            }
            
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (prop.ValueType is PropertyValueType.Float or PropertyValueType.Boolean or PropertyValueType.Integer)
            {
                FlarePseudoProperty pseudoProperty = new(type, null!, new EditorCurveBinding
                {
                    type = type,
                    path = prop.Path,
                    propertyName = prop.Name
                });
                
                FlareProperty flareProperty = new(
                    prop.Name,
                    prop.Path,
                    type,
                    prop.ValueType,
                    prop.ColorType,
                    FlarePropertySource.None,
                    null!,
                    pseudoProperty,
                    null
                );
                
                BindPropertyToAnimatable(controlContext, binder, prop, flareProperty);
            }
            else if (prop.ValueType is PropertyValueType.Vector2 or PropertyValueType.Vector3 or PropertyValueType.Vector4)
            {
                var pseudoProperties = ListPool<FlarePseudoProperty>.Get();

                // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                var iter = prop.ValueType switch
                {
                    PropertyValueType.Vector2 => 2,
                    PropertyValueType.Vector3 => 3,
                    PropertyValueType.Vector4 => 4,
                    _ => throw new ArgumentOutOfRangeException()
                };
                    
                for (int i = 0; i < iter; i++)
                {
                    var isColor = prop.ColorType is PropertyColorType.RGB;
                    var primaryTarget = isColor ? ".r" : ".x";
                    var secondaryTarget = isColor ? ".g" : ".y";
                    var tertiaryTarget = isColor ? ".b" : ".z";
                    var quaternaryTarget = isColor ? ".a" : ".w";

                    var target = i switch
                    {
                        0 => primaryTarget,
                        1 => secondaryTarget,
                        2 => tertiaryTarget,
                        3 => quaternaryTarget,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    
                    pseudoProperties.Add(new FlarePseudoProperty(
                        typeof(float),
                        null!,
                        new EditorCurveBinding
                        {
                            type = type,
                            path = prop.Path,
                            propertyName = $"{prop.Name}{target}"
                        }
                    ));
                }
                
                FlareProperty flareProperty = new(
                    prop.Name,
                    prop.Path,
                    type,
                    prop.ValueType,
                    prop.ColorType,
                    FlarePropertySource.None,
                    null!,
                    null!,
                    pseudoProperties
                );
                
                BindPropertyToAnimatable(controlContext, binder, prop, flareProperty);
                ListPool<FlarePseudoProperty>.Release(pseudoProperties);
            }
        }

        private static string GetId(FlareControl control, FlareAvatarContext context)
        {
            var validId = false;
            var id = $"[Flare] {control.MenuItem.Name}";

            int run = 0;
            while (!validId)
            {
                if (control.Type is not ControlType.Menu || string.IsNullOrWhiteSpace(control.MenuItem.Name))
                {
                    id = $"[Flare] {Guid.NewGuid()}";
                
                    // ReSharper disable once ConvertIfStatementToSwitchStatement
                    if (control.Type is ControlType.PhysBone)
                    {
                        var physBoneInfo = control.PhysBoneInfo;
                        var physBone = physBoneInfo.PhysBone;
                    
                        if (physBone != null)
                        {
                            // If there is a parameter already provided,
                            // we use that instead.
                            var physBoneParameter = physBone.parameter;
                            if (string.IsNullOrWhiteSpace(physBoneParameter))
                                physBone.parameter = id;
                            else
                                id = physBoneParameter;
                        }
                    
                        id += control.PhysBoneInfo.ParameterType switch
                        {
                            PhysBoneParameterType.Stretch => "_Stretch",
                            PhysBoneParameterType.Squish => "_Squish",
                            PhysBoneParameterType.IsGrabbed => "_IsGrabbed",
                            PhysBoneParameterType.IsPosed => "_IsPosed",
                            PhysBoneParameterType.Angle => "_Angle",
                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }
                    else if (control.Type is ControlType.Contact)
                    {
                        var contact = control.ContactInfo.ContactReceiver;
                    
                        // ReSharper disable once InvertIf
                        if (contact != null)
                        {
                            if (string.IsNullOrWhiteSpace(contact.parameter))
                                contact.parameter = id;
                            else
                                id = contact.parameter;
                        }
                    }
                }

                if (run > 0)
                    id += $" {run}";

                validId = context.ControlContexts.All(c => c.Id != id);
                run++;
            }
            return id;
        }
        
        private static void BindPropertyToAnimatable(ControlContext controlContext, BindingService binder, PropertyInfo prop, FlareProperty animatable)
        {
            var path = animatable.Path;
            var type = animatable.ContextType;

            void CreateAnimatableFloatProperty(FlarePseudoProperty flarePseudoProperty, float inverseValue, int index)
            {
                var name = flarePseudoProperty.Name;
                var defaultValue = prop.OverrideDefaultValue ? prop.ValueType switch
                {
                    PropertyValueType.Boolean => prop.OverrideDefaultAnalog,
                    PropertyValueType.Integer => prop.OverrideDefaultAnalog,
                    PropertyValueType.Float => prop.OverrideDefaultAnalog,
                    PropertyValueType.Vector2 => prop.OverrideDefaultVector[index],
                    PropertyValueType.Vector3 => prop.OverrideDefaultVector[index],
                    PropertyValueType.Vector4 => prop.OverrideDefaultVector[index],
                    _ => throw new ArgumentOutOfRangeException()
                } : binder.GetPropertyValue(flarePseudoProperty);
                var targetDefaultValue = prop.State is ControlState.Enabled ? defaultValue : inverseValue;
                var targetInverseValue = prop.State is ControlState.Enabled ? inverseValue : defaultValue;

                AnimatableBinaryProperty property = new(type, path, name, targetDefaultValue, targetInverseValue);
                controlContext.AddProperty(property);
            }

            switch (animatable.Type)
            {
                case PropertyValueType.Float or PropertyValueType.Boolean or PropertyValueType.Integer:
                {
                    var inverseValue = prop.Analog;
                    CreateAnimatableFloatProperty(animatable.GetPseudoProperty(0), inverseValue, 0);
                    break;
                }
                case PropertyValueType.Vector2 or PropertyValueType.Vector3 or PropertyValueType.Vector4:
                {
                    // Need to convert each individual property for vectors to floats
                    CreateAnimatableFloatProperty(animatable.GetPseudoProperty(0), prop.Vector[0], 0); // Guaranteed
                    CreateAnimatableFloatProperty(animatable.GetPseudoProperty(1), prop.Vector[1], 1); // Guaranteed
                    if (animatable.Type is PropertyValueType.Vector3 or PropertyValueType.Vector4)
                        CreateAnimatableFloatProperty(animatable.GetPseudoProperty(2), prop.Vector[2], 2);
                    if (animatable.Type is PropertyValueType.Vector4)
                        CreateAnimatableFloatProperty(animatable.GetPseudoProperty(3), prop.Vector[3], 3);
                    break;
                }
            }
        }
    }
}