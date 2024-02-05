using System;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Flare.Models
{
    [Serializable]
    public class PhysBoneInfo
    {
        [field: SerializeField]
        public VRCPhysBone PhysBone { get; private set; } = null!;

        [field: SerializeField]
        public PhysBoneParameterType ParameterType { get; private set; }
    }
}