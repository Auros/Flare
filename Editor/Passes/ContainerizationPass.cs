using System;
using System.Collections.Generic;
using System.Linq;
using Flare.Editor.Models;
using Flare.Editor.Services;
using Flare.Models;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Flare.Editor.Passes
{
    /// <summary>
    /// Converts control data into corresponding control containers.
    /// </summary>
    internal class ContainerizationPass : Pass<ContainerizationPass>
    {
        private static readonly Dictionary<string, Type?> _qualifierToType = new();
        
        public override string DisplayName => "Containerization";

        protected override void Execute(BuildContext context)
        {
            var avatarRoot = context.AvatarRootTransform;
            var flare = context.GetState<FlareAvatarContext>();

            // (one (1) set of property values). it expensive fr
            // I have an idea on how to optimize this. Generating the material properties is by far
            // the costliest step, so we can first build a list of all the materials on the avatar, compute
            // the bindings for each unique(!) material, and build the path manually from every renderer.
            // Then we can grab the bindings for every component that doesn't use a renderer. We would
            // only be able to do it for GameObjects with renderers WITH components that we know (Transform, etc.)
            // Perhaps we can optimistically collect Type-Binding groups if we ran the renderer exclusionary search
            // first, and then hope that renderers don't contain other materials. At that point if there's components
            // we don't recognize we could run the binding on that specific... fuck as I'm writing this I realized we
            // can do this in a smarter way. I know if you try to create a binding on a path + component that doesn't
            // exist it HARD CRASHES Unity, but maybe the name doesn't matter. We can manually generate the bindings
            // for every GameObject that has a component with the context type of the property. Let's give it a go...
            //
            // 12 minutes later and well I'll be damned it works. So it turns out the hard crash only occures when a
            // non animatable (non-component) object is passed in as the context. ah, eto.... EHHH?
            //
            //BindingService? exclusiveBinder = null;
            //ILookup<string, FlareProperty>? exclusiveBindings = null;

            // We don't need the property gathering from the binder, just the value fetcher.
            BindingService binder = new(context.AvatarRootObject, Array.Empty<GameObject>());
            
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
                    using (ListPool<PropertyInfo>.Get(out var properties))
                    {
                        properties.AddRange(group.Properties);
                        
                        // Full-avatar binding search
                        if (group.SelectionType is PropertySelectionType.Avatar)
                        {
                            foreach (var property in group.Properties)
                            {
                                var propertyType = GetContextType(property);
                                
                                // Make sure to exclude exclusions
                                var candidates = avatarRoot.GetComponentsInChildren(propertyType)
                                    .Where(c => !group.Exclusions.Contains(c.gameObject));
                                
                                properties.AddRange(candidates.Select(c => new PropertyInfo
                                {
                                    Name = property.Name,
                                    ColorType = property.ColorType,
                                    ValueType = property.ValueType,
                                    ContextType = property.ContextType,
                                    Path = c.transform.GetAnimatablePath(avatarRoot),
                                    Analog = property.Analog,
                                    Vector = property.Vector,
                                    OverrideDefaultValue = property.OverrideDefaultValue,
                                    OverrideDefaultAnalog = property.OverrideDefaultAnalog,
                                    OverrideDefaultVector = property.OverrideDefaultVector
                                }));
                            }
                        }
                        
                        foreach (var prop in properties)
                            BindAnimatablePropertiesFast(controlContext, binder, prop);
                    }
                }
            }
        }

        private static Type? GetContextType(PropertyInfo info)
        {
            // ReSharper disable once InvertIf
            if (!_qualifierToType.TryGetValue(info.ContextType, out var type))
            { 
                // Allocations to this can and will add up, this boy loves triggering GC.Alloc and GC.Release
                type = Type.GetType(info.ContextType);
                _qualifierToType[info.ContextType] = type;
            }
            return type;
        }

        // I needed this to be faster so I couldn't directly use the BindingService to get the properties.
        private static void BindAnimatablePropertiesFast(ControlContext controlContext, BindingService binder, PropertyInfo prop)
        {
            var type = GetContextType(prop);
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