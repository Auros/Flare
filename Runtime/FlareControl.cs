using Flare.Models;
using UnityEngine;

namespace Flare
{
    [DisallowMultipleComponent]
    internal class FlareControl : FlareModule
    {
        [field: SerializeField]
        public ControlType Type { get; private set; } = ControlType.Toggle;
    }
}