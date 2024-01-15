using System;
using Flare.Editor.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Inspectors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FlareMenu))]
    internal class FlareMenuInspector : UnityEditor.Editor
    {
        private SerializedProperty? _menuNameProperty;
        private SerializedProperty? _menuIconProperty;
        private SerializedProperty? _synchronizationProperty;
        
        private void OnEnable()
        {
            _menuNameProperty = serializedObject.FindProperty("_menuName");
            _menuIconProperty = serializedObject.FindProperty("_menuIcon");
            _synchronizationProperty = serializedObject.FindProperty("_synchronizeNameWithGameObject");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var component = target as FlareMenu;
            
            VisualElement root = new();
            
            // Setup menu name property.
            PropertyField menuNameField = new(_menuNameProperty);
            root.Add(menuNameField);
            
            // Update the name of the component AND the GameObject if it's changed.
            menuNameField.RegisterValueChangeCallback(ctx =>
            {
                if (component.AsNullable() is null || component!.Synchronize is false)
                    return;

                component.SetName(ctx.changedProperty.stringValue);
            });
            
            // Setup menu icon property.
            root.Add(new PropertyField(_menuIconProperty));

            root.Add(new HorizontalSpacer());
            
            // Setup Advanced Settings
            Foldout advancedSettingsFoldout = new()
            {
                text = "Advanced Settings",
                value = EditorPrefs.GetBool("nexus.auros.flare.advancedSettingsFoldout", false)
            };
            advancedSettingsFoldout.RegisterValueChangedCallback(_ =>
            {
                EditorPrefs.SetBool("nexus.auros.flare.advancedSettingsFoldout", advancedSettingsFoldout.value);
            });
            
            PropertyField synchroPropertyField = new(_synchronizationProperty);
            advancedSettingsFoldout.Add(synchroPropertyField);
            
            root.Add(advancedSettingsFoldout);
            
            return root;
        }
    }
}