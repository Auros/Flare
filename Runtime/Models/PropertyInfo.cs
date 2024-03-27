using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class PropertyInfo
    {
        [field: SerializeField]
        public string Name { get; internal set; } = string.Empty;

        [field: SerializeField]
        public string Path { get; internal set; } = string.Empty;

        [field: SerializeField]
        public PropertyValueType ValueType { get; internal set; }

        [field: SerializeField]
        public PropertyColorType ColorType { get; internal set; }

        [field: SerializeField]
        public string ContextType { get; internal set; } = string.Empty;

        [field: SerializeField]
        public float Analog { get; internal set; }

        [field: SerializeField]
        public Vector4 Vector { get; internal set; }

        [field: SerializeField]
        public UnityEngine.Object Object { get; internal set; }

        [field: SerializeField]
        public string ObjectType { get; internal set; }

        [field: SerializeField]
        public ControlState State { get; internal set; }

        [field: SerializeField]
        public bool OverrideDefaultValue { get; internal set; }

        [field: SerializeField]
        public float OverrideDefaultAnalog { get; internal set; }

        [field: SerializeField]
        public UnityEngine.Object OverrideDefaultObject { get; internal set; }

        [field: SerializeField]
        public Vector4 OverrideDefaultVector { get; internal set; }
    }
}