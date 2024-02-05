using Flare.Editor.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Elements
{
    internal class CategoricalFoldout : Foldout
    {
        // ReSharper disable once InconsistentNaming
        public Toggle toggle { get; }
        
        public CategoricalFoldout()
        {
            toggle = this.GetToggle();
            
            var darkMode = EditorGUIUtility.isProSkin;
            var color = darkMode ? new Color32(0x62, 0x62, 0x62, 0xFF) : new Color32(0xB4, 0xB4, 0xB4, 0xFF);
            
            toggle.style.backgroundColor = (Color)color;
            toggle.WithPadding(2f).WithBorderRadius(4f);
        }
    }
}