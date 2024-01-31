using Flare.Models;
using UnityEngine;

namespace Flare
{
    [DisallowMultipleComponent]
    internal class FlareControl : FlareModule
    {
        [field: SerializeField]
        public ControlType Type { get; private set; }

        [field: SerializeField]
        public MenuItemInfo MenuItem { get; private set; } = new();

        [field: SerializeField]
        public ObjectToggleCollectionInfo ObjectToggleCollection { get; private set; } = new();
        
        [field: SerializeField]
        public PropertyGroupCollectionInfo PropertyGroupCollection { get; private set; } = new();

        [field: SerializeField]
        public LayerInfo LayerInfo { get; private set; } = new();
        
        [field: SerializeField]
        public SettingsInfo Settings { get; private set; } = new();
        
        private void OnValidate()
        {
            LayerInfo.EnsureValidated(gameObject);
            
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
    }
}