using System;
using UnityEditor;
using UnityEngine;

namespace Flare.Editor.Models
{
    internal class FlarePseudoProperty
    {
        private string? _id;
        private string? _internalId;
        
        // This Id is used to create a unique binding path, especially useful when doing lookups as
        // there can be multiple GameObjects with the same property name within a property search.
        public string Id => _id ??= $"{Path}/{Name}";
        
        // also contains the context type
        public string TrueId => _internalId ??= $"{Id}::{ContextType.AssemblyQualifiedName}";
        
        public string Path { get; }
        
        public Type Type { get; }
        
        public string Name { get; }
        
        public Type ContextType { get; }
        
        public GameObject GameObject { get; }
        
        public EditorCurveBinding Binding { get; }
        
        public FlarePseudoProperty(Type type, GameObject gameObject, EditorCurveBinding binding)
        {
            Type = type;
            Binding = binding;
            GameObject = gameObject;
            ContextType = binding.type;
            Name = binding.propertyName;
            Path = binding.path;
        }
    }
}