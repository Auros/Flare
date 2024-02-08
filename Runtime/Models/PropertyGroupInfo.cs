using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class PropertyGroupInfo
    {
        [field: SerializeField]
        public PropertySelectionType SelectionType { get; private set; }

        [field: SerializeField]
        public GameObject?[] Inclusions { get; private set; } = Array.Empty<GameObject?>();
        
        [field: SerializeField]
        public GameObject?[] Exclusions { get; private set; } = Array.Empty<GameObject?>();

        [field: SerializeField]
        public PropertyInfo[] Properties { get; private set; } = Array.Empty<PropertyInfo>();
    }
}