using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    public class FlareTag
    {
        [field: SerializeField]
        public string Value { get; set; } = string.Empty;
    }
}