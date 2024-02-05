using System;
using Flare.Models;
using UnityEngine;

namespace Flare
{
    internal class FlareTags : FlareModule
    {
        [field: SerializeField]
        public string[] Tags { get; private set; } = Array.Empty<string>();

        [field: SerializeField]
        public FlareRule[] Rules { get; private set; } = Array.Empty<FlareRule>();
    }
}