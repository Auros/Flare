using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Flare.Editor.Attributes;
using Flare.Editor.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Inspectors
{
    public abstract class FlareInspector : UnityEditor.Editor
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldInfoCache = new();
        
        private void OnEnable()
        {
            InjectSerializedProperties(this, serializedObject);
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new()
            {
                style =
                {
                    marginLeft = 8f,
                    marginTop = 4f
                }
            };

            var propertyValidationReport = ValidateSerializedProperties(this);
            if (propertyValidationReport.Count is not 0)
            {
                StringBuilder sb = new();
                sb.AppendLine("Please report this!");
                sb.AppendLine("Unable to find serialized property matches for the following:");
                foreach (var missingField in propertyValidationReport)
                    sb.AppendLine(missingField);
                
                root.Add(new HelpBox(sb.ToString(), HelpBoxMessageType.Error));
                return root;
            }

            if (Application.isPlaying)
            {
                root.Add(new HelpBox(
                    "Application is currently in play mode. Any changes will not persist when exiting.",
                    HelpBoxMessageType.Warning)
                );
            }
            
            return BuildUI(root);
        }

        protected abstract VisualElement BuildUI(VisualElement root);

        private static void InjectSerializedProperties(object target, SerializedObject serializedObject)
        {
            var type = target.GetType();
            if (!FieldInfoCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfoCache[type] = fields;
            }

            foreach (var field in fields)
            {
                // Find the target attribute
                var propertyNameAttribute = field.GetCustomAttribute<PropertyNameAttribute>();
                if (propertyNameAttribute is null)
                    continue;

                // Check if it exists as a property or a field.
                var propertyName = propertyNameAttribute.Value;
                var property = serializedObject.Property(propertyName) ?? serializedObject.Field(propertyName);
                
                // If we have the property, set the field to it.
                if (property is null)
                    continue;
                
                field.SetValue(target, property);
            }
        }

        private static IReadOnlyList<string> ValidateSerializedProperties(object target)
        {
            var type = target.GetType();
            if (!FieldInfoCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfoCache[type] = fields;
            }

            List<string> missingFields = new();
            foreach (var field in fields)
            {
                var propertyNameAttribute = field.GetCustomAttribute<PropertyNameAttribute>();
                if (propertyNameAttribute is null)
                    continue;

                // Make sure to use a != to support overridden null operators.
                if (field.GetValue(target) != null)
                    continue;
                
                missingFields.Add($"{propertyNameAttribute.Value} ({field.Name})");
            }
            return missingFields;
        }
    }
}