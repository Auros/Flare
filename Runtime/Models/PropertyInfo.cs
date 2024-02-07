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

        [field: SerializeField]
        public bool OverrideDefaultValue { get; private set; }

        [field: SerializeField]
        public float OverrideDefaultAnalog { get; private set; }

        [field: SerializeField]
        public Vector4 OverrideDefaultVector { get; private set; }
    }
}