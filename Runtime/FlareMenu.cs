using UnityEngine;

namespace Flare
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    internal class FlareMenu : FlareModule
    {   
        [SerializeField]
        private string _menuName;

        [SerializeField]
        private Texture2D _menuIcon;

        [SerializeField]
        private bool _synchronizeNameWithGameObject = true;
        
        public string Name => _menuName;

        public Texture2D Icon => _menuIcon;

        public bool Synchronize => _synchronizeNameWithGameObject;

        private void OnValidate()
        {
            if (!_synchronizeNameWithGameObject)
                return;

            if (name == _menuName)
                return;

            SetName(name);
        }

        private void Update()
        {
            // Ensure the menu name is synchronized with the GameObject name.
            OnValidate();
        }

        public void SetName(string newName)
        {
            name = newName;
            _menuName = newName;
        }
    }
}