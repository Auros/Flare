using System;
using System.Collections.Generic;
using System.Linq;
using Flare.Editor.Models;
using Flare.Models;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace Flare.Editor.Services
{
    internal class BindingService
    {
        private readonly Type[] _types;
        private readonly GameObject _root;
        private readonly GameObject?[] _gameObjects;
        private readonly Dictionary<Type, string> _assemblyQualifiedNames = new();
        private readonly bool _gameObjectSearch;
        
        /// <summary>
        /// Creates a binding service from the provided root that scans for property bindings recursively if
        /// the corresponding GameObject has one of those types.
        /// </summary>
        /// <param name="root">The binding root.</param>
        /// <param name="gameObjects">The GameObjects to ignore.</param>
        /// <param name="types">The types to search for. If </param>
        public BindingService(GameObject root, GameObject?[] gameObjects, params Type[] types)
        {
            _root = root;
            _types = types;
            _gameObjects = gameObjects;
        }
        
        /// <summary>
        /// Creates a binding service from the provided root.
        /// </summary>
        /// <param name="root">The binding root.</param>
        /// <param name="gameObjects">The GameObjects to get the property bindings from.</param>
        public BindingService(GameObject root, params GameObject?[] gameObjects)
        {
            _root = root;
            _types = Array.Empty<Type>();
            _gameObjects = gameObjects;
            _gameObjectSearch = true;
        }

        public T GetPropertyValue<T>(FlareProperty property)
        {
            return (T)GetPropertyValue(property);
        }

        public object GetPropertyValue(FlarePseudoProperty property)
        {
            _ = AnimationUtility.GetFloatValue(_root, property.Binding, out var pseudoDefault);
            return pseudoDefault;
        }

        public object GetPropertyValue(FlareProperty property)
        {
            float GetFloatValue(FlareProperty prop)
            {
                _ = AnimationUtility.GetFloatValue(_root, prop.GetPseudoProperty(0).Binding, out var value);
                return value;
            }
            
            int GetIntValue(FlareProperty prop)
            {
                _ = AnimationUtility.GetDiscreteIntValue(_root, prop.GetPseudoProperty(0).Binding, out var value);
                return value;
            }

            Vector4 GetVectorValue(FlareProperty prop)
            {
                if (prop.Color is PropertyColorType.HDR)
                    _ = true;
                
                var value = Vector4.zero;
                for (int i = 0; i < prop.Length; i++)
                {
                    _ = AnimationUtility.GetFloatValue(_root, prop.GetPseudoProperty(i).Binding, out var floatValue);
                    value[i] = floatValue;
                }
                return value;
            }

            UnityEngine.Object GetObjectValue(FlareProperty prop)
            {
                _ = AnimationUtility.GetObjectReferenceValue(_root, prop.GetPseudoProperty(0).Binding, out var objectValue);
                return objectValue;
            }
            
            // The Allocator 9000
            object value = property.Type switch
            {
                PropertyValueType.Boolean => GetFloatValue(property) > 0,
                PropertyValueType.Integer => GetIntValue(property),
                PropertyValueType.Float => GetFloatValue(property),
                PropertyValueType.Vector2 => GetVectorValue(property),
                PropertyValueType.Vector3 => GetVectorValue(property),
                PropertyValueType.Vector4 => GetVectorValue(property),
                PropertyValueType.Object => GetObjectValue(property),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            return value;
        }
        
        public IEnumerable<FlareProperty> GetPropertyBindings(PropertyInfo[]? preSearch = null, bool pathMatch = true)
        {
            GameObject[] objectsToSearch;
            if (_gameObjects.Length is not 0 && _gameObjectSearch)
                objectsToSearch = _gameObjects.Where(g => g!).Select(g => g!).ToArray();
            else if (_types.Length is not 0)
            {
                objectsToSearch = _types.SelectMany(type =>
                {
                    // Unfortunately we can't use a ListPool for this search as it requires a generic type to search.
                    return _root.GetComponentsInChildren(type, true)
                        .Where(o => !_gameObjects.Contains(o.gameObject))
                        .Select(component => component.gameObject);
                }).Distinct().ToArray(); // Make it distinct first so there's no duplicate GameObjects.
            }
            else
            {
                if (_gameObjectSearch)
                    return Array.Empty<FlareProperty>();
                
                // We won't need this execution path but I still made it for completionism.
                objectsToSearch = _root.GetComponentsInChildren<Component>(true)
                    .Select(component => component.gameObject)
                    .Distinct()
                    .ToArray();
            }

            var pseudoProperties = objectsToSearch.SelectMany(obj =>
            {
                return AnimationUtility.GetAnimatableBindings(obj, _root).Select(binding =>
                {
                    // Ignore EditorOnly properties.
                    if (typeof(IEditorOnly).IsAssignableFrom(binding.type))
                        return null;
                    
                    var type = AnimationUtility.GetEditorCurveValueType(_root, binding);

                    // For the time being we'll only be working with the primitive float, int, and bool properties.
                    // In the future we may be able to support others like Materials and such.
                    if (type != typeof(float) && type != typeof(int) && type != typeof(bool) && type != typeof(Material))
                        return null;
                    
                    if (preSearch == null)
                        return new FlarePseudoProperty(type, obj, binding);
                    
                    bool exists = false;
                    foreach (var pre in preSearch)
                    {
                        if (pathMatch && string.CompareOrdinal(pre.Path, binding.path) is not 0)
                            continue;

                        if (!_assemblyQualifiedNames.TryGetValue(binding.type, out var qualifiedName))
                        {
                            qualifiedName = binding.type.AssemblyQualifiedName;
                            _assemblyQualifiedNames[binding.type] = qualifiedName;
                        }
                        
                        if (string.CompareOrdinal(pre.ContextType, qualifiedName) is not 0)
                            continue;
                        
                        if (!binding.propertyName.StartsWith(pre.Name, StringComparison.Ordinal))
                            continue;
                            
                        exists = true;
                        break;
                    }

                    return !exists ? null : new FlarePseudoProperty(type, obj, binding);
                });
            }).Where(property => property is not null).Select(property => property!).ToArray();
            
            // Convert the pseudo properties into a dictionary, this next step requires a lot of lookups
            // which will get much slower with large amounts of properties (thanks poiyomi shaders).
            var pseudoSearch = pseudoProperties
                .GroupBy(p => p.Id)
                .Select(p => p.First())
                .ToDictionary(property => property.Id, property => property);

            // We look for properties that and with .r or .x so we can combine them into Color or Vector properties.
            var potentialMultiProperties = pseudoProperties
                .Where(property =>
                {
                    // We explicitly look for the property suffixes as .EndsWith
                    // get slow especially with large amounts of properties.
                    var span = property.Name.AsSpan();
                    if (2 > span.Length || span[^2] != '.')
                        return false;

                    var lastCharacter = span[^1];
                    return lastCharacter is 'r' or 'x';
                });

            List<FlareProperty> properties = new();
            
            foreach (var potential in potentialMultiProperties)
            {
                // Make sure we're working with float values
                if (potential.Type != typeof(float))
                    continue;

                // If it's not a color, its a vector :Clueless:
                var isColor = potential.Name[^1] == 'r' && potential.Name[^2] == '.';

                // Remove the .r or .x at the end
                var baseName = potential.Name[..^2];
                var baseProperty = potential.Id[..^2];

                var secondaryTarget = isColor ? ".g" : ".y";
                var tertiaryTarget = isColor ? ".b" : ".z";
                var quaternaryTarget = isColor ? ".a" : ".w";

                // Check if the 2nd, 3rd, and 4th properties exist.
                var secondaryExists = pseudoSearch.TryGetValue($"{baseProperty}{secondaryTarget}", out var second);
                var tertiaryExists = pseudoSearch.TryGetValue($"{baseProperty}{tertiaryTarget}", out var third);
                var quaternaryExists = pseudoSearch.TryGetValue($"{baseProperty}{quaternaryTarget}", out var fourth);

                var type = quaternaryExists
                    ? PropertyValueType.Vector4
                    : tertiaryExists
                        ? PropertyValueType.Vector3
                        : secondaryExists ? PropertyValueType.Vector2 : PropertyValueType.Float;

                // Filtering out non-color HDR like properties from poiyomi, idk what HDR or ST is but they're not
                // actual colors.
                var color = isColor
                    ? PropertyColorType.RGB
                    : baseName.Contains("Color") && !baseName.Contains("HDR") && !baseName.Contains("_ST")
                        ? PropertyColorType.HDR : PropertyColorType.None;
                
                var propsToInclude = new[] { potential, second, third, fourth }.Where(p => p != null);

                FlareProperty property = new(
                    baseName,
                    potential.Path,
                    potential.ContextType,
                    type,
                    color,
                    GetSource(potential),
                    potential.GameObject,
                    null,
                    propsToInclude.ToArray()
                );
                
                properties.Add(property);
            }
            
            // Now that we've collected the vector based properties, we build a dictionary off the included properties
            // to know which properties NOT to add when adding all the other pseudo properties.
            // The Dictionary is for efficiency with larger amounts of properties (once again).
            var includedPseudoSearch = properties
                .SelectMany(p => p.PseudoProperties)
                .GroupBy(p => p.TrueId)
                .Select(p => p.First())
                .ToDictionary(p => p.TrueId, p => p);
            
            foreach (var pseudoProperty in pseudoProperties)
            {
                // Skip over properties we added in the last step.
                if (includedPseudoSearch.ContainsKey(pseudoProperty.TrueId))
                    continue;

                var type = pseudoProperty.Type;
                PropertyValueType propertyType;
                if (type == typeof(float))
                    propertyType = PropertyValueType.Float;
                else if (type == typeof(int))
                    propertyType = PropertyValueType.Integer;
                else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    propertyType = PropertyValueType.Object;
                else
                    propertyType = PropertyValueType.Boolean;

                FlareProperty property = new(
                    pseudoProperty.Name,
                    pseudoProperty.Path,
                    pseudoProperty.ContextType,
                    propertyType,
                    PropertyColorType.None,
                    GetSource(pseudoProperty),
                    pseudoProperty.GameObject,
                    pseudoProperty,
                    null
                );
                
                properties.Add(property);
            }
            
            return properties;
        }

        private static FlarePropertySource GetSource(FlarePseudoProperty pseudoProperty)
        {
            var name = pseudoProperty.Name;
            var context = pseudoProperty.ContextType;
            
            if (typeof(SkinnedMeshRenderer).IsAssignableFrom(context) && name.StartsWith("blendShape."))
                return FlarePropertySource.Blendshape;
            
            if (typeof(Renderer).IsAssignableFrom(context) && name.StartsWith("material."))
                return FlarePropertySource.Material;

            return FlarePropertySource.None;
        }
    }
}