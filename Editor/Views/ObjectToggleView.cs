using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    public class ObjectToggleView : IView
    {
        [PropertyName(nameof(ObjectToggleCollectionInfo.Toggles))]
        private readonly SerializedProperty _togglesProperty = null!;
        
        public void Build(VisualElement root)
        {
            root.CreatePropertyField(_togglesProperty);
        }
    }
}