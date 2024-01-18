using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    public class ObjectToggleInfo
    {
        [field: SerializeField]
        public GameObject Context { get; private set; } = null!;
        
        [field: SerializeField]
        public Behaviour? Target { get; private set; }
        
        [field: SerializeField]
        public ToggleMode Mode { get; private set; }
    }
}