using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class PropertyGroupCollectionInfo
    {
        [field: SerializeField]
        public PropertyGroupInfo[] Groups { get; private set; } = Array.Empty<PropertyGroupInfo>();
    }
}