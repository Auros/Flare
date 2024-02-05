using System.Collections.Generic;
using Flare.Models;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Flare
{
    [DisallowMultipleComponent]
    internal class FlareControl : FlareModule
    {
        [field: SerializeField]
        public ControlType Type { get; private set; }

        [field: SerializeField]
        public InterpolationInfo Interpolation { get; private set; } = new();

        [field: SerializeField]
        public MenuItemInfo MenuItem { get; private set; } = new();

        [field: SerializeField]
        public PhysBoneInfo PhysBoneInfo { get; private set; } = new();

        [field: SerializeField]
        public ContactInfo ContactInfo { get; private set; } = new();
        
        [field: SerializeField]
        public ObjectToggleCollectionInfo ObjectToggleCollection { get; private set; } = new();
        
        [field: SerializeField]
        public PropertyGroupCollectionInfo PropertyGroupCollection { get; private set; } = new();

        [field: SerializeField]
        public TagInfo TagInfo { get; private set; } = new();
        
        [field: SerializeField]
        public SettingsInfo Settings { get; private set; } = new();
        
        private void OnValidate()
        {
            TagInfo.EnsureValidated(gameObject);
            
            foreach (var toggle in ObjectToggleCollection.Toggles)
                toggle.EnsureValidated();
            
            EditorControllers.Get<IFlareModuleHandler<FlareControl>>(nameof(FlareControl)).Add(this);
            
            // Only update the name if synchronization is on
            // and if the names aren't matching.
            if (!Settings.SynchronizeName || name == MenuItem.Name)
                return;

            SetName(name);
        }

        private void OnDestroy()
        {
            EditorControllers.Get<IFlareModuleHandler<FlareControl>>(nameof(FlareControl)).Remove(this);
        }

        public void SetName(string newName)
        {
            name = newName;
            MenuItem.Name = newName;
        }

        public void GetReferencesNotOnAvatar(ICollection<Object?> references)
        {
            var descriptor = transform.GetComponentInParent<VRCAvatarDescriptor>();
            if (!descriptor)
                return;

            var root = descriptor.transform;
            foreach (var toggle in ObjectToggleCollection.Toggles)
            {
                var target = toggle.GetTargetTransform();
                SearchTransform(root, target, toggle.Target, references);
            }

            foreach (var group in PropertyGroupCollection.Groups)
            {
                var search = group.SelectionType is PropertySelectionType.Normal ? group.Inclusions : group.Exclusions;
                foreach (var item in search)
                {
                    if (item == null)
                        continue;
                    
                    SearchTransform(root, item.transform, item, references);
                }
            }
        }

        private static void SearchTransform(Object root, Transform? target, Object? source, ICollection<Object?> references)
        {
            if (target == null)
                return;

            bool isInAvatar = false;
            while (target != target.root)
            {
                target = target.parent;
                if (target != root)
                    continue;
                    
                isInAvatar = true;
                break;
            }
                
            if (isInAvatar)
                return;
                
            references.Add(source);
        }
    }
}