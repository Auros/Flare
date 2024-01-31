using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    public class FlareLayer
    {
        [field: SerializeField]
        public string Value { get; set; } = string.Empty;
    }
}