﻿using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class MenuItemInfo
    {
        [field: SerializeField]
        public string Name { get; set; } = string.Empty;

        [field: SerializeField]
        public Texture Icon { get; private set; } = null!;

        [field: SerializeField]
        public MenuItemType Type { get; private set; } = MenuItemType.Toggle;
        
        [field: SerializeField]
        public bool DefaultState { get; private set; }
        
        [field: Range(0, 1)]
        [field: SerializeField]
        public float DefaultRadialValue { get; private set; }
    }
}