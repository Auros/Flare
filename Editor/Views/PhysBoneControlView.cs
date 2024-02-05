using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    internal class PhysBoneControlView : IView
    {
        [PropertyName(nameof(PhysBoneInfo.PhysBone))]
        private readonly SerializedProperty _physBoneProperty = null!;

        [PropertyName(nameof(PhysBoneInfo.ParameterType))]
        private readonly SerializedProperty _parameterTypeProperty = null!;
        
        public void Build(VisualElement root)
        {
            root.CreatePropertyField(_physBoneProperty);
            root.CreatePropertyField(_parameterTypeProperty);
            root.CreateHorizontalSpacer(10f);
        }
    }
}