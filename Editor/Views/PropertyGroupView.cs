using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    public class PropertyGroupView : IView
    {
        [PropertyName(nameof(PropertyGroupCollectionInfo.Groups))]
        private readonly SerializedProperty _groupsProperty = null!;
        
        public void Build(VisualElement root)
        {
            root.CreatePropertyField(_groupsProperty);
        }
    }
}