using System.Linq;
using Flare.Editor.Models;
using Flare.Editor.Services;
using Flare.Models;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Flare.Editor.Passes
{
    /// <summary>
    /// Converts control data into corresponding control containers.
    /// </summary>
    public class ContainerizationPass : Pass<ContainerizationPass>
    {
        protected override void Execute(BuildContext context)
        {
            var flare = context.GetState<FlareAvatarContext>();

            foreach (var control in flare.Controls)
            {
                ControlContext controlContext = new(control);
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
                        GameObject go => (go.transform.GetAnimatablePath(context.AvatarRootTransform), go.activeSelf),
                        Component component => (component.transform.GetAnimatablePath(context.AvatarRootTransform), (component as Behaviour).AsNullable()?.enabled ?? (component as Renderer).AsNullable()?.enabled),
                        _ => (null, false)
                    };

                    if (path is null || defaultValue is null)
                    {
                        // TODO: Report insigificant error.
                        // NDMF has an error report API but there's barely any docs on how to use it properly.
                        continue;
                    }

                    float defaultFloatValue;
                    float inverseFloatValue;
                    if (toggle.MenuMode is ToggleMode.Enabled)
                    {
                        defaultFloatValue = defaultValue.Value ? 1f : 0f;
                        inverseFloatValue = toggle.ToggleMode is ToggleMode.Enabled ? 1f : 0f;
                    }
                    else
                    {
                        defaultFloatValue = toggle.ToggleMode is ToggleMode.Enabled ? 1f : 0f;
                        inverseFloatValue = defaultValue.Value ? 1f : 0f;
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
                    if (group.SelectionType is PropertySelectionType.Normal)
                    {
                        // This could be more optimized, but for now I'll leave it like this for simplicity (I'm tired.)
                        var include = group.Inclusions;
                        BindingService binder = new(root, include);
                        var bindings = binder.GetPropertyBindings().Where(b => b.GameObject != root).ToArray();
                        
                        foreach (var prop in group.Properties)
                        {
                            foreach (var animatable in bindings)
                            {
                                if (animatable.Type != prop.ValueType
                                    || animatable.Name != prop.Name
                                    || animatable.Path != prop.Path
                                    || animatable.ContextType.AssemblyQualifiedName != prop.ContextType)
                                    continue;
                                
                                // We found our property
                                BindPropertyToAnimatable(controlContext, binder, prop, animatable);
                            }
                        }

                    }
                    else if (group.SelectionType is PropertySelectionType.Avatar)
                    {
                        var exclude = group.Exclusions;
                        
                        BindingService binder = new(root, exclude, typeof(Renderer), typeof(VRCPhysBone));
                        var bindings = binder.GetPropertyBindings()
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
        
        private static void BindPropertyToAnimatable(ControlContext controlContext, BindingService binder, PropertyInfo prop, FlareProperty animatable)
        {
            var path = animatable.Path;
            var type = animatable.ContextType;

            void CreateAnimatableFloatProperty(FlarePseudoProperty flarePseudoProperty, float inverseValue)
            {
                var name = flarePseudoProperty.Name;
                var defaultValue = binder.GetPropertyValue(flarePseudoProperty);
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
                    CreateAnimatableFloatProperty(animatable.PseudoProperties[0], inverseValue);
                    break;
                }
                case PropertyValueType.Vector2 or PropertyValueType.Vector3 or PropertyValueType.Vector4:
                {
                    // Need to convert each individual property for vectors to floats
                    CreateAnimatableFloatProperty(animatable.PseudoProperties[0], prop.Vector[0]); // Guaranteed
                    CreateAnimatableFloatProperty(animatable.PseudoProperties[1], prop.Vector[1]); // Guaranteed
                    if (animatable.Type is PropertyValueType.Vector3 or PropertyValueType.Vector4)
                        CreateAnimatableFloatProperty(animatable.PseudoProperties[2], prop.Vector[2]);
                    if (animatable.Type is PropertyValueType.Vector4)
                        CreateAnimatableFloatProperty(animatable.PseudoProperties[3], prop.Vector[3]);
                    break;
                }
            }
        }
    }
}