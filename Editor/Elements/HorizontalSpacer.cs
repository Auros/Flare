using UnityEngine.UIElements;

namespace Flare.Editor.Elements
{
    internal class HorizontalSpacer : VisualElement
    {
        public HorizontalSpacer(float height = 8f)
        {
            style.height = height;
        }
    }
}