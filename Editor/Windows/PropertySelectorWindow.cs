using System;
using System.Linq;
using Flare.Editor.Elements;
using Flare.Editor.Extensions;
using Flare.Editor.Models;
using Flare.Editor.Services;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDKBase;
using Debug = UnityEngine.Debug;
using PropertyInfo = Flare.Models.PropertyInfo;

namespace Flare.Editor.Windows
{
    internal class PropertySelectorWindow : EditorWindow
    {
        private SerializedProperty? _target;
        
        public static void Present(SerializedProperty property)
        {
            var window = GetWindow<PropertySelectorWindow>();
            window.titleContent = new GUIContent("Flare Property Selector");
            window._target = property;
            
            var screenPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            window.position = new Rect(screenPos - new Vector2(200, 30f), new Vector2(500, 550));
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

        private static void CreateSelector(VisualElement root, SerializedProperty property)
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
                PropertySelectionType.Avatar => Array.Empty<GameObject>(), // TODO: Recursive search for avatar components... gulp.
                _ => throw new ArgumentOutOfRangeException()
            }).Distinct().ToArray();
            
            var avatarGameObject = descriptor.gameObject;
            root.CreateLabel($"Current Avatar: {avatarGameObject.name}").WithPadding(3f);

            BindingService binder = new(avatarGameObject, objects);

            ToolbarSearchField searchField = new();
            root.Add(searchField);
            searchField.style.width = new StyleLength(StyleKeyword.Auto);

            var buttonGroup = root.CreateHorizontal();
            buttonGroup.CreateButton("Blendshapes", () => searchField.value = "t:Blendshape ").WithGrow(1f);
            buttonGroup.CreateButton("Materials", () => searchField.value = "t:Material ").WithGrow(1f);
            buttonGroup.CreateButton("Everything", () => searchField.value = string.Empty).WithGrow(1f);

            var bindings = binder.GetPropertyBindings().ToArray();
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
                    _ = pseudoProperty;
                }

                cell.SetData(binding, binder.GetPropertyValue(binding), () =>
                {
                    // On Set
                    Debug.Log($"{binding.Name} ({binding.Type}) ({binding.Path}) {binding.Source}");
                }, () =>
                {
                    // Jump To Source
                    Selection.activeGameObject = binding.GameObject;
                },
                    // Context Menu Actions
                    isVector ? () => HandlePseudoProperty(0) : null,
                    isVector ? () => HandlePseudoProperty(1) : null,
                    binding.Type is PropertyValueType.Vector3 or PropertyValueType.Vector4 ? () => HandlePseudoProperty(2) : null,
                    binding.Type is PropertyValueType.Vector4  ? () => HandlePseudoProperty(3) : null
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
            _target = null;
        }
    }
}