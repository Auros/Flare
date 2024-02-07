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
        public float Analog { get; set; }

        [field: SerializeField]
        public Vector4 Vector { get; set; }

        [field: SerializeField]
        public ControlState State { get; set; }

        [field: SerializeField]
        public bool OverrideDefaultValue { get; set; }

        [field: SerializeField]
        public float OverrideDefaultAnalog { get; set; }

        [field: SerializeField]
        public Vector4 OverrideDefaultVector { get; set; }
    }
}