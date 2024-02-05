using UnityEngine;

namespace Flare
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    internal class FlareMenu : FlareModule
    {   
        [field: SerializeField]
        public string Name { get; private set; } = string.Empty;

        [field: SerializeField]
        public Texture2D Icon { get; private set; } = null!;

        [field: SerializeField]
        public bool Synchronize { get; private set; } = true;
        
        public void SetName(string newName)
        {
            name = newName; 
            Name = newName;
        }
  
        private void OnValidate()
        {
            EditorControllers.Get<IFlareModuleHandler<FlareMenu>>(nameof(FlareMenu)).Add(this);
            
            // Only update the name if synchronization is on
            // and if the names aren't matching.
            if (!Synchronize || name == Name)
                return;

            SetName(name);
        }

        private void OnDestroy()
        {
            EditorControllers.Get<IFlareModuleHandler<FlareMenu>>(nameof(FlareMenu)).Remove(this);
        }
    }
}