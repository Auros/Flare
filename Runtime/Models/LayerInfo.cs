using System;
using UnityEngine;
using VRC.SDKBase;

namespace Flare.Models
{
    [Serializable]
    internal class LayerInfo
    {
        [field: SerializeField]
        public FlareLayerModule? Module { get; private set; }
        
        [field: SerializeField]
        public FlareLayer[] Layers { get; private set; } = Array.Empty<FlareLayer>();

        public bool EnsureValidated(GameObject gameObject)
        {
            if (Layers.Length is 0)
                return false;

            var descriptor = gameObject.GetComponentInParent<VRC_AvatarDescriptor>();
            if (!descriptor)
                return false;

            var layerModule = descriptor.GetComponentInChildren<FlareLayerModule>();
            if (layerModule)
                return true;

            GameObject module = new("Flare Layer Module");
            module.transform.SetParent(descriptor.transform);
            Module = module.AddComponent<FlareLayerModule>();
            
            var moduleTransform = module.transform;
            moduleTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            moduleTransform.localScale = Vector3.one;
            
            return true;
        }
    }
}