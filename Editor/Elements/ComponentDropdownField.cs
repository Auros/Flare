using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Flare.Editor.Elements
{
    // UI Toolkit binding :woozy_face:
    internal class ComponentDropdownField : PopupField<Object>
    {
        public ComponentDropdownField(GameObject? gameObject) : base(
            gameObject.AsNullable()?.GetComponents<Component>()?.Select(c => (Object)c).ToList() ?? new List<Object>(0),
            0,
            FormatSelectedComponent,
            FormatListComponent
            )
        {
            
        }

        public void Push(Object? target)
        {
            var gameObject = target as GameObject;
            if (gameObject is null && target is Component component)
                gameObject = component.gameObject;
            
            choices = gameObject.AsNullable()?.GetComponents<Component>()?.Select(c => (Object)c).ToList() ??
                      new List<Object>(0);
        }

        private static string FormatSelectedComponent(Object component)
        {
            if (component == null)
                return "<null>";
            
            // ReSharper disable once ConvertIfStatementToReturnStatement
            // If it's the transform, we treat it as the GameObject
            if (component is Transform transform && component == transform)
                return nameof(GameObject);

            return component.GetType().Name;
        }

        private static string FormatListComponent(Object component)
        {
            if (component == null)
                return "<null>";
            
            // ReSharper disable once ConvertIfStatementToReturnStatement
            // If it's the transform, we treat it as the GameObject
            if (component is Transform transform && component == transform)
                return $"{transform.gameObject.name} (GameObject)";

            return component.ToString();
        }
    }
}