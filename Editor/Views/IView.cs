using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    internal interface IView
    {
        void Build(VisualElement root);
    }
}