using System;
using System.Collections.Generic;
using System.Linq;
using Flare.Models;
using UnityEngine;

namespace Flare.Editor.Models
{
    internal class FlareProperty
    {
        public string Id { get; }
        
        public string Name { get; }
        
        public string Path { get; }
        
        public Type ContextType { get; }
        
        public PropertyValueType Type { get; }
        
        public PropertyColorType Color { get; }
        
        public GameObject GameObject { get; }
        
        public FlarePropertySource Source { get; }
        
        public IReadOnlyList<FlarePseudoProperty> PseudoProperties { get; }

        public FlareProperty(string name, string path, Type contextType, PropertyValueType type, PropertyColorType color,
            FlarePropertySource source, GameObject gameObject, params FlarePseudoProperty[] pseudoProperties)
        {
            Type = type;
            Name = name;
            Path = path;
            Color = color;
            Source = source;
            GameObject = gameObject;
            ContextType = contextType;
            PseudoProperties = pseudoProperties.ToArray();
            Id = $"{Path}/{Name}";
        }
    }
}