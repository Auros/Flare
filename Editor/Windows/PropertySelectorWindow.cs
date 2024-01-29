﻿using System;
using System.Linq;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Editor.Models;
using Flare.Editor.Services;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
using PropertyInfo = Flare.Models.PropertyInfo;

namespace Flare.Editor.Windows
{
    internal class PropertySelectorWindow : EditorWindow
    {
        private SerializedProperty? _target;
        private static Rect? _lastSize;
        
        public static void Present(SerializedProperty property)
        {
            var window = GetWindow<PropertySelectorWindow>();
            window.titleContent = new GUIContent("Flare Property Selector");
            window._target = property;

            var screenPos = GUIUtility.GUIToScreenPoint(
                Event.current != null ? Event.current.mousePosition : Vector3.zero
            );
            _lastSize ??= new Rect(screenPos - new Vector2(200, 30f), new Vector2(500, 550));
            window.position = _lastSize.Value;
        }
        
        private void CreateGUI()
        {
            VisualElement unconfiguredElement = new();
            unconfiguredElement.WithName("Unconfigured");
            unconfiguredElement.CreateLabel("Loading Properties...");
            unconfiguredElement.WithPadding(5f);
            rootVisualElement.Add(unconfiguredElement);

            VisualElement root = new();
            root.WithPadding(5f);
            root.WithName("Root");
            rootVisualElement.Add(root);
            root.style.display = DisplayStyle.None;

        }

        private void CreateSelector(VisualElement root, SerializedProperty property)
        {
            if (Selection.activeObject is not GameObject gameObject)
                return;

            var descriptor = gameObject.GetComponentInParent<VRC_AvatarDescriptor>();
            if (!descriptor)
                return;

            var groupsProperty = property.serializedObject
                .Property(nameof(FlareControl.PropertyGroupCollection))!;

            // Writing this makes me uncomfortable... Unity Editor dev is pain.
            var propertyGroups = ((PropertyGroupCollectionInfo)groupsProperty.boxedValue).Groups;
            
            // Use the property path to find the index of the Property Group
            // In my opinion, this is pretty cursed, but it's guaranteed to work.
            var pathSpan = property.propertyPath.AsSpan();
            var startBracketIndex = pathSpan.IndexOf('[');
            var endingBracketIndex = pathSpan.IndexOf(']');
            var groupIndexString = pathSpan.Slice(startBracketIndex + 1, endingBracketIndex - startBracketIndex - 1);
            var groupIndex = int.Parse(groupIndexString);

            var propertyGroup = propertyGroups[groupIndex];

            var objects = (propertyGroup.SelectionType switch
            {
                PropertySelectionType.Normal => propertyGroup.Inclusions,
                PropertySelectionType.Avatar => propertyGroup.Exclusions,
                _ => throw new ArgumentOutOfRangeException()
            }).Distinct().ToArray();
            
            var avatarGameObject = descriptor.gameObject;
            root.CreateLabel($"Current Avatar: {avatarGameObject.name}").WithPadding(3f);

            var binder = propertyGroup.SelectionType is PropertySelectionType.Normal ?
                new BindingService(avatarGameObject, objects)
                : new BindingService(avatarGameObject, objects, typeof(Renderer), typeof(VRCPhysBone));

            ToolbarSearchField searchField = new();
            root.Add(searchField);
            searchField.style.width = new StyleLength(StyleKeyword.Auto);

            var buttonGroup = root.CreateHorizontal();
            buttonGroup.CreateButton("Blendshapes", () => searchField.value = "t:Blendshape ").WithGrow(1f);
            buttonGroup.CreateButton("Materials", () => searchField.value = "t:Material ").WithGrow(1f);
            buttonGroup.CreateButton("Everything", () => searchField.value = string.Empty).WithGrow(1f);

            var bindingSearch = binder.GetPropertyBindings().Where(b => b.GameObject != avatarGameObject);
            if (propertyGroup.SelectionType is PropertySelectionType.Avatar)
                bindingSearch = bindingSearch.GroupBy(p => p.Name).Select(g => g.First());

            var bindings = bindingSearch.ToArray();
            var items = bindings.ToList();

            ListView list = new(items, 46f, () => new BindablePropertyCell(), (e, i) =>
            {
                if (e is not BindablePropertyCell cell)
                    return;

                var binding = items[i];
                var isVector = binding.Type
                    is PropertyValueType.Vector2
                    or PropertyValueType.Vector3
                    or PropertyValueType.Vector4;
                
                // ReSharper disable once MoveLocalFunctionAfterJumpStatement
                void HandlePseudoProperty(int index)
                {
                    if (index > binding.PseudoProperties.Count)
                        return;

                    var pseudoProperty = binding.PseudoProperties[index];
                    SetProperty(pseudoProperty);
                }

                void SetProperty(FlarePseudoProperty? pseudoProperty = null)
                {
                    var propName = pseudoProperty?.Name ?? binding.Name;
                    var type = pseudoProperty is null ? binding.Type : PropertyValueType.Float;
                    var color = pseudoProperty is null ? binding.Color : PropertyColorType.None;
                    var contextType = binding.ContextType;
                    
                    property.Property(nameof(PropertyInfo.Name)).SetValue(propName);
                    property.Property(nameof(PropertyInfo.Path)).SetValue(binding.Path);
                    property.Property(nameof(PropertyInfo.ValueType)).SetValue(type);
                    property.Property(nameof(PropertyInfo.ColorType)).SetValue(color);
                    property.Property(nameof(PropertyInfo.ContextType)).SetValue(contextType.AssemblyQualifiedName!);

                    switch (type)
                    {
                        // Seed the default value to start with
                        case PropertyValueType.Float or PropertyValueType.Boolean or PropertyValueType.Integer:
                        {
                            var defaultValue = binder.GetPropertyValue(binding);
                            if (defaultValue is bool boolValue)
                                defaultValue = boolValue ? 1f : 0f;

                            if (defaultValue is Vector4 or Vector3 or Vector2)
                            {
                                _ = AnimationUtility.GetFloatValue(avatarGameObject, pseudoProperty!.Binding, out var pseudoDefault);
                                defaultValue = pseudoDefault;
                            }
                            
                            defaultValue = (float)defaultValue;
                            property.Property(nameof(PropertyInfo.Analog)).SetValue(defaultValue);
                            break;
                        }
                        case PropertyValueType.Vector2 or PropertyValueType.Vector3 or PropertyValueType.Vector4:
                        {
                            var defaultValue = (Vector4)binder.GetPropertyValue(binding);
                            property.Property(nameof(PropertyInfo.Vector)).SetValue(defaultValue);
                            break;
                        }
                    }
                    
                    Close();
                }

                var isTriOrQuadVector = binding.Type is PropertyValueType.Vector3 or PropertyValueType.Vector4;
                
                cell.SetData
                    (binding,
                    binder.GetPropertyValue(binding),
                    () => SetProperty(),
                    () => Selection.activeGameObject = binding.GameObject,
                    
                    // Context Menu Actions
                    isVector ? () => HandlePseudoProperty(0) : null,
                    isVector ? () => HandlePseudoProperty(1) : null,
                    isTriOrQuadVector ? () => HandlePseudoProperty(2) : null,
                    binding.Type is PropertyValueType.Vector4 ? () => HandlePseudoProperty(3) : null
                );
            });

            searchField.RegisterValueChangedCallback(ctx =>
            {
                // TODO: Add optimistic search when typing (no backspace)
                items.Clear();
                if (ctx.newValue == string.Empty)
                {
                    items.AddRange(bindings);
                    list.RefreshItems();
                    return;
                }
                
                var search = ctx.newValue;
                var materialsOnly = search.Contains("t:Material");
                var blendshapesOnly = search.Contains("t:Blendshape");

                search = search
                    .Replace("t:Material", string.Empty)
                    .Replace("t:Blendshape", string.Empty)
                    .Trim();

                var searchParts = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var group in bindings)
                {
                    var failedSearch = false;
                    var source = group.Source;

                    if (materialsOnly && source is not FlarePropertySource.Material)
                        continue;
                    
                    if (blendshapesOnly && source is not FlarePropertySource.Blendshape)
                        continue;
                    
                    foreach (var part in searchParts)
                    {
                        if (group.Id.Contains(part, StringComparison.OrdinalIgnoreCase))
                            continue;

                        failedSearch = true;
                        break;
                    }

                    if (failedSearch)
                        continue;
                    
                    items.Add(group);
                }

                list.RefreshItems();
                list.Query<BindablePropertyCell>().ForEach(BindablePropertyCell.FixSelector);
            });

            VisualElement container = new();
            container.Add(list);
            root.Add(container);
            container.WithGrow(1f);
            container.style.height = new StyleLength(StyleKeyword.Null);
            
            list.selectionType = SelectionType.None;
            searchField.value = "t:Blendshape ";
        }

        private void OnLostFocus()
        {
            Close();
        }

        private void OnInspectorUpdate()
        {
            if (_target == null)
                return;

            var rootElement = rootVisualElement.Q<VisualElement>("Root");
            var unconfiguredElement = rootVisualElement.Q<VisualElement>("Unconfigured");
            unconfiguredElement.style.display = DisplayStyle.None;

            if (rootElement.style.display == DisplayStyle.None)
            {
                rootElement.style.display = DisplayStyle.Flex;
                CreateSelector(rootElement, _target);
            }
            
            // Dismiss if there's no valid property.
            // TODO: Also dismiss if they click off the window
            // The goal is to mimic the AnimationCurve property drawer window thingymajig
            try
            {
                // Can't find another way to do this unfortunately, but if we try to access a property
                // when the serialized property is destroyed (array element deleted or exited/reselected component,
                // it will throw once.
                _ = _target.Property(nameof(PropertyInfo.Path));
            }
            catch
            {
                _target = null;
                Close();
            }
        }

        private void OnDisable()
        {
            _lastSize = position;
            _target = null;
        }
    }
}