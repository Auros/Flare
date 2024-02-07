using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class MenuItemInfo
    {
        [field: SerializeField]
        public string Name { get; set; } = string.Empty;

        [field: SerializeField]
        public Texture2D Icon { get; private set; } = null!;

        [field: SerializeField]
        public MenuItemType Type { get; private set; } = MenuItemType.Toggle;

        [field: SerializeField]
        public bool IsSaved { get; private set; } = true;
        
        [field: SerializeField]
        public bool DefaultState { get; private set; }
        
        [field: Range(0, 1)]
        [field: SerializeField]
        public float DefaultRadialValue { get; private set; }
        
        [field: SerializeField]
        public bool IsTagTrigger { get; private set; }

        [field: SerializeField]
        public string Tag { get; private set; } = string.Empty;
        
        [field: SerializeField]
        public ToggleMode TriggerMode { get; private set; }
    }
}