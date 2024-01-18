using System.Linq;
using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    public class ObjectToggleView : IView
    {
        [PropertyName(nameof(ObjectToggleCollectionInfo.Toggles))]
        private readonly SerializedProperty _togglesProperty = null!;
        
        public void Build(VisualElement root)
        {
            var togglesProperty = root.CreatePropertyField(_togglesProperty)
                .WithLabel("Toggles");
            
            // q.RemoveFromClassList(ObjectField.alignedFieldUssClassName);
        }
    }
}