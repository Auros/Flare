using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class FlareRule
    {
        [field: SerializeField]
        public string CauseLayer { get; private set; } = string.Empty;

        [field: SerializeField]
        public string EffectLayer { get; private set; } = string.Empty;

        [field: SerializeField]
        public ToggleMode CauseState { get; private set; } = ToggleMode.Enabled;

        [field: SerializeField]
        public ToggleMode EffectState { get; private set; } = ToggleMode.Disabled;
    }
}