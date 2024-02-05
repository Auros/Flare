using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Elements
{
    internal class PaneMenu : VisualElement
    {
        private static Texture2D? _paneOptionsImage;
        private Action<ContextualMenuPopulateEvent>? _populator;
        
        public PaneMenu(Action<ContextualMenuPopulateEvent>? populator = null)
        {
            if (!_paneOptionsImage)
                _paneOptionsImage = (Texture2D)EditorGUIUtility.Load(GetPaneOptionsImage());

            _populator = populator;
            style.backgroundImage = _paneOptionsImage;
            ContextualMenuManipulator menu = new(ModifyContextMenu);
            menu.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            this.AddManipulator(menu);
        }

        private void ModifyContextMenu(ContextualMenuPopulateEvent evt)
        {
            _populator?.Invoke(evt);
        }

        public void SetData(Action<ContextualMenuPopulateEvent>? populator)
        {
            _populator = populator;
        }
        
        private static string GetPaneOptionsImage()
        {
            // There might be a better way to do this, but this is the simplest for me right now as I can
            // only find examples on using the pane options icon via USS.
            var skin = EditorGUIUtility.isProSkin ? "DarkSkin" : "LightSkin";
            return $"Builtin Skins/{skin}/Images/pane options.png";
        }
    }
}