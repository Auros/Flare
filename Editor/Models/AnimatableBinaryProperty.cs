using System;

namespace Flare.Editor.Models
{
    public class AnimatableBinaryProperty
    {
        public Type Type { get; }
        
        public string Path { get; }
        
        public string Name { get; }
        
        public float DefaultValue { get; }
        
        public float InverseValue { get; }

        public AnimatableBinaryProperty(Type type, string path, string name, float defaultValue, float inverseValue)
        {
            Type = type;
            Path = path;
            Name = name;
            DefaultValue = defaultValue;
            InverseValue = inverseValue;
        }

        public void Deconstruct(out string path, out Type type, out string name, out float defaultValue, out float inverseValue)
        {
            path = Path;
            type = Type;
            name = Name;
            defaultValue = DefaultValue;
            inverseValue = InverseValue;
        }
    }
}