using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Flare.Editor.Models;
using Flare.Editor.Services;
using Flare.Models;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using PropertyInfo = Flare.Models.PropertyInfo;

namespace Flare.Editor.Passes
{
    /// <summary>
    /// Converts control data into corresponding control containers.
    /// </summary>
    internal class ContainerizationPass : Pass<ContainerizationPass>
    {
        private static readonly Dictionary<Type, FieldInfo[]> _fields = new();
        private static readonly Dictionary<string, Type?> _qualifierToType = new();
        
        public override string DisplayName => "Containerization";

        protected override void Execute(BuildContext context)
        {
            var avatarRoot = context.AvatarRootTransform;
            var flare = context.GetState<FlareAvatarContext>();
            
            if (flare.IsEmpty)
                return;

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
            // I'm leaving this here just because...
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
                    if (toggle.Target == null)
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

                    bool? overrideActivity = null;
                    if (control.Type is ControlType.Menu && control.MenuItem.Type is MenuItemType.Toggle or MenuItemType.Radial)
                    {
                        overrideActivity = (int)toggle.MenuMode is not 1 ==
                                           (control.MenuItem.Type is MenuItemType.Toggle
                                               ? control.MenuItem.DefaultState
                                               : control.MenuItem.DefaultRadialValue > 0);

                        overrideActivity = overrideActivity.Value
                            ? toggle.ToggleMode is ToggleMode.Enabled
                            : toggle.AlternateMode is ToggleMode.Enabled;
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

                        if (overrideActivity.HasValue)
                        {
                            switch (toggle.Target)
                            {
                                case Renderer renderer:
                                    renderer.enabled = overrideActivity.Value;
                                    break;
                                case Behaviour behaviour:
                                    behaviour.enabled = overrideActivity.Value;
                                    break;
                            }
                        }
                    }
                    else if (overrideActivity.HasValue && toggle.Target is GameObject go)
                    {
                        go.SetActive(overrideActivity.Value);
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
                                if (propertyType == null)
                                    continue;
                                
                                // Specifically do not support the GameObject toggle in SelectionType Avatar
                                // (why would you want to toggle on and off *every gameobject*?
                                // + its annoying to impl
                                // If someone reading this wants this feature just hmu -Auros
                                if (propertyType == typeof(GameObject))
                                    continue; // MAYBE: Warn unsupported feature?
                                
                                // Make sure to exclude exclusions
                                var candidates = avatarRoot.GetComponentsInChildren(propertyType, true)
                                    .Where(c => !group.Exclusions.Contains(c.gameObject));
                                
                                properties.AddRange(candidates.Select(c => new PropertyInfo
                                {
                                    Name = property.Name,
                                    State = property.State,
                                    ColorType = property.ColorType,
                                    ValueType = property.ValueType,
                                    ContextType = property.ContextType,
                                    Path = c.transform.GetAnimatablePath(avatarRoot),
                                    
                                    Analog = property.Analog,
                                    Vector = property.Vector,
                                    Object = property.Object,
                                    ObjectType = property.ObjectType,
                                    OverrideDefaultValue = property.OverrideDefaultValue,
                                    OverrideDefaultAnalog = property.OverrideDefaultAnalog,
                                    OverrideDefaultVector = property.OverrideDefaultVector,
                                    OverrideDefaultObject = property.OverrideDefaultObject,
                                }));
                            }
                        }
                        
                        foreach (var prop in properties)
                            BindAnimatablePropertiesFast(flare, context, controlContext, binder, prop);
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
        private static void BindAnimatablePropertiesFast(FlareAvatarContext flare, BuildContext buildContext, ControlContext controlContext, BindingService binder, PropertyInfo prop)
        {
            var type = GetContextType(prop);
            if (type == null)
            {
                // MAYBE: Warn?
                return;
            }

            var propertyContext = flare.GetPropertyContext(prop);
            var propertyPath = propertyContext.AsNullable()?.transform.GetAnimatablePath(buildContext.AvatarRootTransform);

            if (propertyPath == null)
            {
                propertyPath = prop.Path;
                Debug.Log("no component found for " + prop.Path);
            }

            if (propertyPath != prop.Path)
                Debug.Log("Moved '" + prop.Path + "' to '" + propertyPath + "'.");

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (prop.ValueType is PropertyValueType.Float or PropertyValueType.Boolean or PropertyValueType.Integer)
            {
                FlarePseudoProperty pseudoProperty = new(type, null!, new EditorCurveBinding
                {
                    type = type,
                    path = propertyPath,
                    propertyName = prop.Name
                });
                
                FlareProperty flareProperty = new(
                    prop.Name,
                    propertyPath,
                    type,
                    prop.ValueType,
                    prop.ColorType,
                    FlarePropertySource.None,
                    null!,
                    pseudoProperty,
                    null
                );
                
                BindPropertyToAnimatable(flare, controlContext, binder, prop, flareProperty);
            }
            else if (prop.ValueType is PropertyValueType.Object)
            {
                // i really hate using type.gettype, remove this later if we dont need binding type
                FlarePseudoProperty pseudoProperty = new(Type.GetType(prop.ObjectType), null!, new EditorCurveBinding
                {
                    type = type,
                    path = propertyPath,
                    propertyName = prop.Name
                });

                FlareProperty flareProperty = new(
                    prop.Name,
                    propertyPath,
                    type,
                    prop.ValueType,
                    prop.ColorType,
                    FlarePropertySource.None,
                    null!,
                    pseudoProperty,
                    null
                );

                BindPropertyToAnimatable(flare, controlContext, binder, prop, flareProperty);
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
                            path = propertyPath,
                            propertyName = $"{prop.Name}{target}"
                        }
                    ));
                }
                
                FlareProperty flareProperty = new(
                    prop.Name,
                    propertyPath,
                    type,
                    prop.ValueType,
                    prop.ColorType,
                    FlarePropertySource.None,
                    null!,
                    null!,
                    pseudoProperties
                );
                
                BindPropertyToAnimatable(flare, controlContext, binder, prop, flareProperty);
                ListPool<FlarePseudoProperty>.Release(pseudoProperties);
            }
        }

        private static string GetId(FlareControl control, FlareAvatarContext flare)
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

                validId = flare.ControlContexts.All(c => c.Id != id);
                run++;
            }
            return id;
        }
        
        private static void BindPropertyToAnimatable(FlareAvatarContext flare, ControlContext controlContext, BindingService binder, PropertyInfo prop, FlareProperty animatable)
        {
            var path = animatable.Path;
            var type = animatable.ContextType;

            switch (animatable.Type)
            {
                case PropertyValueType.Float or PropertyValueType.Boolean or PropertyValueType.Integer:
                {
                    var inverseValue = prop.Analog;
                    CreateAnimatableProperty(animatable.GetPseudoProperty(0), inverseValue, 0);
                    break;
                }
                case PropertyValueType.Vector2 or PropertyValueType.Vector3 or PropertyValueType.Vector4:
                {
                    // Need to convert each individual property for vectors to floats
                    CreateAnimatableProperty(animatable.GetPseudoProperty(0), prop.Vector[0], 0); // Guaranteed
                    CreateAnimatableProperty(animatable.GetPseudoProperty(1), prop.Vector[1], 1); // Guaranteed
                    if (animatable.Type is PropertyValueType.Vector3 or PropertyValueType.Vector4)
                        CreateAnimatableProperty(animatable.GetPseudoProperty(2), prop.Vector[2], 2);
                    if (animatable.Type is PropertyValueType.Vector4)
                        CreateAnimatableProperty(animatable.GetPseudoProperty(3), prop.Vector[3], 3);
                    break;
                }
                case PropertyValueType.Object:
                {
                    var inverseValue = prop.Object;
                    CreateAnimatableProperty(animatable.GetPseudoProperty(0), inverseValue, 0);
                    break;
                }
            }
            
            void CreateAnimatableProperty(FlarePseudoProperty flarePseudoProperty, object inverseValue, int index)
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
                    PropertyValueType.Object => prop.OverrideDefaultObject,
                    _ => throw new ArgumentOutOfRangeException()
                } : binder.GetPropertyValue(flarePseudoProperty);
                
                var targetDefaultValue = prop.State is ControlState.Enabled ? defaultValue : inverseValue;
                var targetInverseValue = prop.State is ControlState.Enabled ? inverseValue : defaultValue;

                if (prop.OverrideDefaultValue)
                {
                    // Automatically assign these values on the base on upload for avatar preview.
                    TryAssignDefaultToAvatar(flare, defaultValue, type, prop, name, index);
                }

                AnimatableBinaryProperty property = new(type, path, name, targetDefaultValue, targetInverseValue);
                controlContext.AddProperty(property);
            }

        }

        private static void TryAssignDefaultToAvatar(FlareAvatarContext flare, object value, Type contextType, PropertyInfo propertyInfo, string propertyName, int index)
        {
            var source = flare.GetPropertyContext(propertyInfo);
            if (source == null)
                return;

            if (contextType == typeof(GameObject))
            {
                if (propertyName == "m_IsActive")
                    source.gameObject.SetActive((float)value >= 0.5f);
                
                return;
            }

            if (contextType == typeof(Transform))
            {
                var transform = source.transform;
                switch (propertyInfo.Name)
                {
                    case "m_LocalPosition":
                    {
                        var localPosition = transform.localPosition;
                        localPosition[index] = (float)value;
                        transform.localPosition = localPosition;
                        break;
                    }
                    case "m_LocalRotation":
                    {
                        var localRotation = transform.localRotation;
                        localRotation[index] = (float)value;
                        transform.localRotation = localRotation;
                        break;
                    }
                    case "m_LocalScale":
                    {
                        var localScale = transform.localScale;
                        localScale[index] = (float)value;
                        transform.localScale = localScale;
                        break;
                    }
                }
                return;
            }

            if (propertyInfo.Name == "m_Enabled")
            {
                var component = source.GetComponent(contextType);
                switch (component)
                {
                    case Behaviour behaviour:
                        behaviour.enabled = (float)value > 0.5f;
                        break;
                    case Renderer renderer:
                        renderer.enabled = (float)value > 0.5f;
                        break;
                }
                return;
            }

            if (propertyInfo.Name.StartsWith("m_Materials.Array.data[", StringComparison.Ordinal))
            {
                var matIndexStr = propertyInfo.Name.Substring(23).Trim();
                matIndexStr = matIndexStr.Remove(matIndexStr.Length - 1);
                var matIndex = int.Parse(matIndexStr);

                var component = source.GetComponent(contextType);
                if (component is Renderer renderer)
                {
                    var mats = renderer.materials;
                    mats[matIndex] = (Material)value;
                    renderer.materials = mats;
                }
                return;
            }

            const string blendShapeId = "blendShape.";
            if (typeof(SkinnedMeshRenderer).IsAssignableFrom(contextType) && propertyName.StartsWith(blendShapeId))
            {
                var blendShapeName = propertyName[blendShapeId.Length..];
                var skinnedMeshRenderer = source.GetComponent<SkinnedMeshRenderer>();
                
                if (skinnedMeshRenderer == null) // who knows maybe something funky happened
                    return;

                var blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
                if (blendShapeIndex is -1)
                    return;
                
                skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (float)value);
                
                return;
            }
            
            var contexts = ListPool<Component>.Get();
            source.GetComponents(contextType, contexts);
            
            foreach (var context in contexts)
            {
                if (!_fields.TryGetValue(contextType, out var fields))
                {
                    fields = contextType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    _fields.Add(contextType, fields);
                }

                if (fields.Length is 0)
                    continue;

                var field = fields[0];
                var fieldType = field.FieldType;
                
                if (fieldType == typeof(float))
                    field.SetValue(context, value);
                else if (fieldType == typeof(int))
                    field.SetValue(context, (int)value);
                else if (fieldType == typeof(bool))
                    field.SetValue(context, (float)value >= 0.5f);
                else if (propertyInfo.ValueType is PropertyValueType.Vector2 or PropertyValueType.Vector3 or PropertyValueType.Vector4)
                {
                    if (fieldType == typeof(Vector2))
                    {
                        var vector = (Vector2)field.GetValue(context);
                        vector[index] = (float)value;
                        field.SetValue(context, vector);
                    }
                    else if (fieldType == typeof(Vector3))
                    {
                        var vector = (Vector3)field.GetValue(context);
                        vector[index] = (float)value;
                        field.SetValue(context, vector);
                    }
                    else if (fieldType == typeof(Vector4))
                    {
                        var vector = (Vector4)field.GetValue(context);
                        vector[index] = (float)value;
                        field.SetValue(context, vector);
                    }
                    else if (fieldType == typeof(Color))
                    {
                        var color = (Color)field.GetValue(context);
                        color[index] = (float)value;
                        field.SetValue(context, color);
                    }
                }
            }
            
            ListPool<Component>.Release(contexts);
        } 
    }
}