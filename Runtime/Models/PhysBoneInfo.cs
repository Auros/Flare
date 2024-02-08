using System;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Flare.Models
{
    [Serializable]
    internal class PhysBoneInfo
    {
        [field: SerializeField]
        public VRCPhysBone? PhysBone { get; private set; }

        [field: SerializeField]
        public PhysBoneParameterType ParameterType { get; private set; }
    }
}