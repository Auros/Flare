﻿using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class ObjectToggleCollectionInfo
    {
        [field: SerializeField]
        public ObjectToggleInfo[] Toggles { get; private set; } = Array.Empty<ObjectToggleInfo>();
    }
}