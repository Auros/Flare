using System;
using System.Collections.Generic;
using Flare.Models;
using UnityEngine;

namespace Flare.Editor.Models
{
    internal class FlareProperty
    {
        private string? _id;
        private string? _internalId;

        public string Id => _id ??= $"{Path}/{Name}";

        public string Name { get; }
        
        public string Path { get; }
        
        public Type ContextType { get; }
        
        public PropertyValueType Type { get; }
        
        public PropertyColorType Color { get; }
        
        public GameObject GameObject { get; }
        
        public FlarePropertySource Source { get; }
        
        private FlarePseudoProperty PseudoProperty { get; }
        
        internal IReadOnlyList<FlarePseudoProperty>? PseudoProperties { get; }
        
        // Only used for matching in UI, basically is a unique property name for a specific type and name.
        public string QualifiedId => _internalId ??= $"{Name}::{ContextType.AssemblyQualifiedName}";

        public int Length => PseudoProperties?.Count ?? 1;

        public FlareProperty(string name, string path, Type contextType, PropertyValueType type, PropertyColorType color,
            FlarePropertySource source, GameObject gameObject, FlarePseudoProperty? pseudoProperty, IReadOnlyList<FlarePseudoProperty>? pseudoProperties)
        {
            Type = type;
            Name = name;
            Path = path;
            Color = color;
            Source = source;
            GameObject = gameObject;
            ContextType = contextType;

            PseudoProperty = pseudoProperty!;
            PseudoProperties = pseudoProperties;

            if (pseudoProperty == null && pseudoProperties != null)
                PseudoProperty = pseudoProperties[0];
        }

        public FlarePseudoProperty GetPseudoProperty(int index)
        {
            if (index == 0)
                return PseudoProperty;

            if (PseudoProperties is null)
                throw new InvalidOperationException($"Cannot find pseudo property with index {index}");

            return PseudoProperties[index];
        }
    }
}