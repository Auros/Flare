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
        public override VisualElement CreateInspectorGUI()
        {
            var component = target as FlareMenu;
            
            VisualElement root = new();

            var menuNameProperty = serializedObject.FindProperty("_menuName");
            var menuIconProperty = serializedObject.FindProperty("_menuIcon");
            var synchronizationProperty = serializedObject.FindProperty("_synchronizeNameWithGameObject");

            PropertyField menuNameField = new(menuNameProperty);
            
            root.Add(menuNameField);
            root.Add(new PropertyField(menuIconProperty));

            root.Add(new HorizontalSpacer());
            
            Foldout advancedSettingsFoldout = new()
            {
                text = "Advanced Settings",
                value = false
            };
            
            // Update the name of the component AND the GameObject if it's changed.
            menuNameField.RegisterValueChangeCallback(ctx =>
            {
                if (component.AsNullable() is null || component!.Synchronize is false)
                    return;

                component.SetName(ctx.changedProperty.stringValue);
            });

            PropertyField synchroPropertyField = new(synchronizationProperty);
            advancedSettingsFoldout.Add(synchroPropertyField);
            
            root.Add(advancedSettingsFoldout);
            
            return root;
        }
    }
}