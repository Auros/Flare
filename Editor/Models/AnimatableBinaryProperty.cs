using System;

namespace Flare.Editor.Models
{
    internal class AnimatableBinaryProperty
    {
        public Type Type { get; }
        
        public string Path { get; }
        
        public string Name { get; }
        
        public object DefaultValue { get; }
        
        public object InverseValue { get; }

        public AnimatableBinaryProperty(Type type, string path, string name, object defaultValue, object inverseValue)
        {
            Type = type;
            Path = path;
            Name = name;
            DefaultValue = defaultValue;
            InverseValue = inverseValue;
        }

        public void Deconstruct(out string path, out Type type, out string name, out object defaultValue, out object inverseValue)
        {
            path = Path;
            type = Type;
            name = Name;
            defaultValue = DefaultValue;
            inverseValue = InverseValue;
        }
    }
}