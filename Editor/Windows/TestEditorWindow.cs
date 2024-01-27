using System;
using Flare.Editor.Extensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Windows
{
    public class TestEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Flare/Debug Window")]
        public static void DebugWindow()
        {
            var window = GetWindow<TestEditorWindow>();
            window.titleContent = new GUIContent("Debug Window");
        }

        private void CreateGUI()
        {
            ObjectField objectField = new();
            objectField.WithName("ObjectPropertyField");
            rootVisualElement.Add(objectField);
            
            rootVisualElement.Add(new Button(PrintProperties) { text = "Print Object Properties" });
        }

        private void PrintProperties()
        {
            var field = rootVisualElement.Q<ObjectField>("ObjectPropertyField");
            var value = field.value;
            if (value is not GameObject gameObject)
                return;

            var editorBindings = AnimationUtility.GetAnimatableBindings(gameObject, gameObject);
            foreach (var binding in editorBindings)
                Debug.Log(binding.propertyName);
        }
    }
}