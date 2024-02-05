using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    public class PropertyGroupCollectionView : IView
    {
        [PropertyName(nameof(PropertyGroupCollectionInfo.Groups))]
        private readonly PropertyGroupView _propertyGroupView = new();

        public void Build(VisualElement root)
        {
            _propertyGroupView.Build(root);
            root.CreateHorizontalSpacer(20f);
        }
    }
}