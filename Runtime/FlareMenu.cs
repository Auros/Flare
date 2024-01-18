using UnityEngine;

namespace Flare
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    internal class FlareMenu : FlareModule
    {   
        [SerializeField]
        private string _menuName = string.Empty;

        [SerializeField]
        private Texture2D _menuIcon = null!;

        [SerializeField]
        private bool _synchronizeNameWithGameObject = true;
        
        public string Name => _menuName;

        public Texture2D Icon => _menuIcon;

        public bool Synchronize => _synchronizeNameWithGameObject;

        public void SetName(string newName)
        {
            name = newName; 
            _menuName = newName;
        }
  
        private void OnValidate()
        {
            EditorControllers.Get<IFlareModuleHandler<FlareMenu>>(nameof(FlareMenu)).Add(this);
            
            // Only update the name if synchronization is on
            // and if the names aren't matching.
            if (!_synchronizeNameWithGameObject || name == _menuName)
                return;

            SetName(name);
        }

        private void OnDestroy()
        {
            EditorControllers.Get<IFlareModuleHandler<FlareMenu>>(nameof(FlareMenu)).Remove(this);
        }
    }
}