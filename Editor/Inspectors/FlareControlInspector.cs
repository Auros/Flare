using System;
using System.Text;
using Flare.Editor.Attributes;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Editor.Views;
using Flare.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Flare.Editor.Inspectors
{
    [CustomEditor(typeof(FlareControl))]
    internal class FlareControlInspector : FlareInspector
    {
        [PropertyName(nameof(FlareControl.Type))]
        private readonly SerializedProperty _typeProperty = null!;
        
        [PropertyName(nameof(FlareControl.Interpolation))]
        private readonly SerializedProperty _interpolationProperty = null!;

        [PropertyName(nameof(FlareControl.MenuItem))]
        private MenuItemControlView? _menuItemControlView;

        [PropertyName(nameof(FlareControl.PhysBoneInfo))]
        private readonly PhysBoneControlView _physBoneControlView = new();

        [PropertyName(nameof(FlareControl.ContactInfo))]
        private readonly ContactControlView _contactControlView = new();

        [PropertyName(nameof(FlareControl.ObjectToggleCollection))]
        private ObjectToggleCollectionView? _objectToggleCollectionView;
        
        [PropertyName(nameof(FlareControl.PropertyGroupCollection))]
        private readonly PropertyGroupCollectionView _propertyGroupCollectionView = new();
        
        [PropertyName(nameof(FlareControl.TagInfo))]
        private readonly TagInfoView _tagInfoView = new();

        protected override void OnInitialization()
        {
            if (target is not FlareControl control)
                return;
            
            _menuItemControlView ??= new MenuItemControlView(control);
            _objectToggleCollectionView ??= new ObjectToggleCollectionView(control);
        }

        protected override VisualElement BuildUI(VisualElement root)
        {
            HelpBox misconfigurationErrorBox = new();
            root.Add(misconfigurationErrorBox);
            misconfigurationErrorBox.Visible(false);
            
            var typeField = root.CreatePropertyField(_typeProperty);
            root.CreatePropertyField(_interpolationProperty.Property(nameof(InterpolationInfo.Duration)))
                .WithTooltip("The duration (in seconds) this control takes to execute. A value of \"0\" means instant. This can also be called interpolation.");
            
            CategoricalFoldout menuItemFoldout = new() { text = "Control (Menu)" };
            _menuItemControlView?.Build(menuItemFoldout);
            root.Add(menuItemFoldout);
            
            CategoricalFoldout physBoneFoldout = new() { text = "Control (PhysBone)" };
            _physBoneControlView.Build(physBoneFoldout);
            root.Add(physBoneFoldout);

            CategoricalFoldout contactFoldout = new() { text = "Control (Contact)" };
            _contactControlView.Build(contactFoldout);
            root.Add(contactFoldout);

            var controlFoldouts = new[] { menuItemFoldout, physBoneFoldout, contactFoldout };
            void UpdateFoldout(ControlType type)
            {
                foreach (var foldout in controlFoldouts)
                    foldout.Visible(false);

                var targetFoldout = type switch
                {
                    ControlType.Menu => menuItemFoldout,
                    ControlType.PhysBone => physBoneFoldout,
                    ControlType.Contact => contactFoldout,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };

                targetFoldout.Visible(true);
            }
            
            typeField.RegisterValueChangeCallback(evt => UpdateFoldout((ControlType)evt.changedProperty.enumValueIndex));
            UpdateFoldout((ControlType)_typeProperty.enumValueIndex);

            CategoricalFoldout toggleFoldout = new() { text = "Object Toggles" };
            _objectToggleCollectionView?.Build(toggleFoldout);
            root.Add(toggleFoldout);

            CategoricalFoldout propertyFoldout = new() { text = "Property Groups" };
            _propertyGroupCollectionView.Build(propertyFoldout);
            root.Add(propertyFoldout);
            
            CategoricalFoldout tagFoldout = new() { text = "Tags", value = false };
            _tagInfoView.Build(tagFoldout);
            root.Add(tagFoldout);

            // Display warning for if any object references are not on this avatar.
            root.schedule.Execute(() =>
            {
                if (target is not FlareControl control)
                    return;

                var notOnAvatar = ListPool<Object?>.Get();
                control.GetReferencesNotOnAvatar(notOnAvatar);

                var errors = notOnAvatar.Count is not 0;
                misconfigurationErrorBox.Visible(errors);
                if (errors)
                {
                    StringBuilder sb = new();
                    sb.AppendLine("Some references are not located under the current avatar.");
                    
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < notOnAvatar.Count; i++)
                    {
                        var invalidObject = notOnAvatar[i];
                        if (invalidObject != null)
                            sb.AppendLine(invalidObject.ToString());
                    }

                    misconfigurationErrorBox.messageType = HelpBoxMessageType.Error;
                    misconfigurationErrorBox.text = sb.ToString();
                }
                
                ListPool<Object?>.Release(notOnAvatar);
                
            }).Every(20);
            
            return root;
        }
    }
}