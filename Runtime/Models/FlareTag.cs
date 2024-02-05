using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class FlareTag
    {
        [field: SerializeField]
        public string Value { get; set; } = string.Empty;
    }
}