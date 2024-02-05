using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Flare.Editor.Attributes;
using Flare.Editor.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Inspectors
{
    internal abstract class FlareInspector : UnityEditor.Editor
    {
        private static readonly Dictionary<Type, FieldInfo[]> _fieldInfoCache = new();

        protected virtual void OnInitialization() { }
        
        private void OnEnable()
        {
            OnInitialization();
            InjectSerializedProperties(this,
                propertyName => serializedObject.Property(propertyName) ?? serializedObject.Field(propertyName)
            );
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

            List<string> propertyValidationReport = new();
            ValidateSerializedProperties(this, propertyValidationReport);
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
        
        private static void InjectSerializedProperties(object target, Func<string, SerializedProperty?> locator)
        {
            var type = target.GetType();
            if (!_fieldInfoCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                _fieldInfoCache[type] = fields;
            }

            foreach (var field in fields)
            {
                // Find the target attribute
                var propertyNameAttribute = field.GetCustomAttribute<PropertyNameAttribute>();
                if (propertyNameAttribute is null)
                    continue;
                
                // Check if it exists as a property or a field.
                var propertyName = propertyNameAttribute.Value;
                var property = locator(propertyName);
                
                // If we have the property, set the field to it.
                if (property is null)
                    continue;
                
                // Process either SerializedProperty or nested views
                if (field.FieldType == typeof(SerializedProperty))
                {
                    field.SetValue(target, property);
                }
                else if (typeof(IView).IsAssignableFrom(field.FieldType))
                {
                    var view = field.GetValue(target);
                    
                    // Ignore null views
                    if (view is null)
                        continue;

                    // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                    InjectSerializedProperties(view, prop => property.Property(prop) ?? property.Field(prop));
                }
            }
        }

        private static void ValidateSerializedProperties(object target, ICollection<string> missing)
        {
            var type = target.GetType();
            if (!_fieldInfoCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                _fieldInfoCache[type] = fields;
            }

            foreach (var field in fields)
            {
                var propertyNameAttribute = field.GetCustomAttribute<PropertyNameAttribute>();
                if (propertyNameAttribute is null)
                    continue;

                // Process either SerializedProperty or nested views
                if (field.FieldType == typeof(SerializedProperty))
                {
                    // Make sure to use a != to support overridden null operators.
                    if (field.GetValue(target) != null)
                        continue;
                    
                    missing.Add($"{propertyNameAttribute.Value} ({field.Name})");
                }
                else if (typeof(IView).IsAssignableFrom(field.FieldType))
                {
                    var view = field.GetValue(target);
                    
                    // Ignore null views
                    if (view is null)
                        continue;

                    ValidateSerializedProperties(view, missing);
                }
            }
        }
    }
}