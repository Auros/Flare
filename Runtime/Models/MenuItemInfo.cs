using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class MenuItemInfo
    {
        [field: SerializeField]
        public string Name { get; set; }
        
        [field: SerializeField]
        public Texture Icon { get; private set; }

        [field: SerializeField]
        public MenuItemType Type { get; private set; } = MenuItemType.Toggle;
        
        [field: SerializeField]
        public float DefaultValue { get; private set; }
    }
}