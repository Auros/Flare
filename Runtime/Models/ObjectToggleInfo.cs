using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flare.Models
{
    [Serializable]
    internal class ObjectToggleInfo
    {
        [field: SerializeField]
        public Object? Target { get; private set; }
        
        [field: SerializeField]
        public ToggleMode MenuMode { get; set; }
        
        [field: SerializeField]
        public ToggleMode ToggleMode { get; set; }

        /// <summary>
        /// Ensures that only GameObjects and Behaviours, and Renderers can be provided for the object target.
        /// </summary>
        public void EnsureValidated()
        {
            if (Target == null || Target is Behaviour || Target is Renderer || Target is GameObject)
                return;

            if (Target is Transform transform)
                Target = transform.gameObject;
            else
            {
                Debug.LogWarning($"Unrecognized type {Target!.GetType().Name}");
                Target = null;
            }
        }

        public Transform? GetTargetTransform()
        {
            return Target switch
            {
                GameObject go => go.transform,
                Component component => component.transform,
                _ => null
            };
        }
    }
}