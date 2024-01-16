using System;
using UnityEngine;

namespace Flare.Models
{
    [Serializable]
    internal class SettingsInfo
    {
        /// <summary>
        /// Synchronize the name of the menu with the name of the GameObject
        /// </summary>
        [field: SerializeField]
        public bool SynchronizeName { get; private set; } = true;
    }
}