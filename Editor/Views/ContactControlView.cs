using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using Flare.Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flare.Editor.Views
{
    internal class ContactControlView : IView
    {
        [PropertyName(nameof(ContactInfo.ContactReceiver))]
        private readonly SerializedProperty _contactProperty = null!;
        
        public void Build(VisualElement root)
        {
            root.CreatePropertyField(_contactProperty);
            root.CreateHorizontalSpacer(10f);
        }
    }
}