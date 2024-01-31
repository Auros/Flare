using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    public class ObjectToggleCollectionView : IView
    {
        [PropertyName(nameof(ObjectToggleCollectionInfo.Toggles))]
        private readonly ObjectToggleView _objectToggleView = new();
        
        public void Build(VisualElement root)
        {
            _objectToggleView.Build(root);
            root.CreateHorizontalSpacer(5f);
        }
    }
}