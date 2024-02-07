using UnityEngine;

namespace Flare
{
    [DisallowMultipleComponent]
    internal class FlareSettings : FlareModule
    {
        [field: SerializeField]
        public bool WriteDefaults { get; set; }
    }
}