using System;
using Flare.Models;
using UnityEngine;

namespace Flare
{
    internal class FlareLayerModule : FlareModule
    {
        [field: SerializeField]
        public string[] Layers { get; private set; } = Array.Empty<string>();

        [field: SerializeField]
        public FlareRule[] Rules { get; private set; } = Array.Empty<FlareRule>();
    }
}