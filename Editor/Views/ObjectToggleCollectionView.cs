using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    internal class ObjectToggleCollectionView : IView
    {
        [PropertyName(nameof(ObjectToggleCollectionInfo.Toggles))]
        private readonly ObjectToggleView _objectToggleView;

        public ObjectToggleCollectionView(FlareControl flareControl)
        {
            _objectToggleView = new ObjectToggleView(flareControl);
        }
        
        public void Build(VisualElement root)
        {
            _objectToggleView.Build(root);
            root.CreateHorizontalSpacer(20f);
        }
    }
}