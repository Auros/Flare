using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class PropertyInfo
    {
        [field: SerializeField]
        public string Name { get; set; } = string.Empty;
        
        [field: SerializeField]
        public string Path { get; set; } = string.Empty;
        
        [field: SerializeField]
        public PropertyValueType ValueType { get; set; }
        
        [field: SerializeField]
        public PropertyColorType ColorType { get; set; }

        [field: SerializeField]
        public string ContextType { get; set; } = string.Empty;

        [field: SerializeField]
        public float Analog { get; private set; }
        
        [field: SerializeField]
        public Vector4 Vector { get; private set; }
        
        [field: SerializeField]
        public ControlState State { get; private set; }
        
        public T GetPropertyValue<T>()
        {
            // The Allocator 9000
            object value = ValueType switch
            {
                PropertyValueType.Boolean => Analog > 0,
                PropertyValueType.Integer => (int)Analog,
                PropertyValueType.Float => Analog,
                PropertyValueType.Vector2 => new Vector2(Vector.x, Vector.y),
                PropertyValueType.Vector3 => new Vector3(Vector.x, Vector.y, Vector.z),
                PropertyValueType.Vector4 => new Vector4(Vector.x, Vector.y, Vector.z, Vector.w),
                _ => throw new ArgumentOutOfRangeException()
            };
            return (T)value;
        }
    }
}