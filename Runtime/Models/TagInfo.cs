using System;
using UnityEngine;
using VRC.SDKBase;

namespace Flare.Models
{
    [Serializable]
    internal class TagInfo
    {
        [field: SerializeField]
        public FlareTags? Module { get; private set; }
        
        [field: SerializeField]
        public FlareTag[] Tags { get; private set; } = Array.Empty<FlareTag>();

        public bool EnsureValidated(GameObject gameObject, bool skipLengthCheck = false)
        {
            if (Tags.Length is 0 && !skipLengthCheck)
                return false;

            var descriptor = gameObject.GetComponentInParent<VRC_AvatarDescriptor>();
            if (!descriptor)
                return false;

            var layerModule = descriptor.GetComponentInChildren<FlareTags>();
            if (layerModule)
                return true;

            GameObject module = new("Flare Tag Module");
            module.transform.SetParent(descriptor.transform);
            Module = module.AddComponent<FlareTags>();
            
            var moduleTransform = module.transform;
            moduleTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            moduleTransform.localScale = Vector3.one;
            
            return true;
        }
    }
}