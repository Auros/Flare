using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Flare.Editor.Controllers
{
    /// <summary>
    /// Synchronizes the GameObject names with the menu item names
    /// for FlareMenu and FlareControl properties.
    /// </summary>
    internal class FlareMenuNameSyncController<T> : IFlareModuleHandler<T> where T : FlareModule
    {
        private readonly Dictionary<GameObject, T> _modules = new();

        public void Add(T module) => _modules[module.gameObject] = module;

        public void Remove(T module) => _modules.Remove(module.gameObject);
        
        protected FlareMenuNameSyncController()
        {
            Undo.postprocessModifications += OnModification;
        }

        public string Id => typeof(T).Name;

        protected virtual void Process(T module) { }

        private UndoPropertyModification[] OnModification(UndoPropertyModification[] modifications)
        {
            foreach (var modification in modifications)
            {
                var value = modification.currentValue;

                // We only care for when GameObjects get renamed
                if (value.target is not GameObject gameObject)
                    continue;
                
                // Specifically check the m_Name property.
                if (modification.currentValue.propertyPath != "m_Name")
                    continue;
                
                // And finally, ensure that we're not in the prefab view.
                if (PrefabStageUtility.GetPrefabStage(gameObject))
                    continue;

                if (!_modules.TryGetValue(gameObject, out var module))
                    continue;
                
                if (!module)
                {
                    Debug.LogWarning($"Null {Id} in sync controller");
                    continue;
                }
                
                Process(module);
            }

            return modifications;
        }
    }
    
    internal class FlareMenuSync : FlareMenuNameSyncController<FlareMenu>
    {
        protected override void Process(FlareMenu module) => module.SetName(module.gameObject.name);
    }
        
    internal class FlareControlSync : FlareMenuNameSyncController<FlareControl>
    {
        protected override void Process(FlareControl module) => module.SetName(module.gameObject.name);
    }
}