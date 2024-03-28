using System;
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
using UnityEngine.Pool;
using UnityEngine.UIElements;
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
            if (property.serializedObject.targetObject is not FlareControl flareCOntrol)
                return;

            var descriptor = flareCOntrol.GetComponentInParent<VRC_AvatarDescriptor>();
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

            // keeping this here in case we ever find a way to differentiate between context type on the UI.
            // for now, renderer type properties will automatically be merged.
            //var mergeRenderersToggle = new Toggle();
            //mergeRenderersToggle.style.marginLeft = StyleKeyword.Auto;
            //mergeRenderersToggle.label = "Combine Renderer Properties";
            //mergeRenderersToggle.WithFontSize(11f);
            //mergeRenderersToggle.WithPadding(2f);
            //mergeRenderersToggle.value = true;

            var avatarGameObject = descriptor.gameObject;
            var headingGroup = root.CreateHorizontal();
            headingGroup.CreateLabel($"Current Avatar: {avatarGameObject.name}").WithPadding(3f);
            //headingGroup.Add(mergeRenderersToggle);

            var binder = propertyGroup.SelectionType is PropertySelectionType.Normal ?
                new BindingService(avatarGameObject, objects)
                : new BindingService(avatarGameObject, objects, typeof(Renderer), typeof(Behaviour));

            ToolbarSearchField searchField = new();
            root.Add(searchField);
            searchField.style.width = new StyleLength(StyleKeyword.Auto);

            var buttonGroup = root.CreateHorizontal();
            buttonGroup.CreateButton("Blendshapes", () => searchField.value = "t:Blendshape ").WithGrow(1f);
            buttonGroup.CreateButton("Materials", () => searchField.value = "t:Material ").WithGrow(1f);
            buttonGroup.CreateButton("Everything", () => searchField.value = string.Empty).WithGrow(1f);
            searchField.Focus();

            var bindingSearch = binder.GetPropertyBindings().Where(b => b.GameObject != avatarGameObject);
            if (propertyGroup.SelectionType is PropertySelectionType.Avatar)
                bindingSearch = bindingSearch.GroupBy(p => p.QualifiedId).Select(g => g.First());

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
                    if (index > binding.Length)
                        return;

                    var pseudoProperty = binding.GetPseudoProperty(index);
                    SetProperty(pseudoProperty);
                }

                void SetProperty(FlarePseudoProperty? pseudoProperty = null)
                {
                    var propName = pseudoProperty?.Name ?? binding.Name;
                    var type = pseudoProperty is null ? binding.Type : PropertyValueType.Float;
                    var color = pseudoProperty is null ? binding.Color : PropertyColorType.None;
                    //var contextType = mergeRenderersToggle.value && typeof(Renderer).IsAssignableFrom(binding.ContextType) ? typeof(Renderer) : binding.ContextType;
                    var contextType = typeof(Renderer).IsAssignableFrom(binding.ContextType) ? typeof(Renderer) : binding.ContextType;

                    property.Property(nameof(PropertyInfo.Name)).SetValue(propName);
                    property.Property(nameof(PropertyInfo.Path)).SetValue(binding.Path);
                    property.Property(nameof(PropertyInfo.ValueType)).SetValue(type);
                    property.Property(nameof(PropertyInfo.ColorType)).SetValue(color);
                    property.Property(nameof(PropertyInfo.ContextType)).SetValue(contextType.AssemblyQualifiedName!);

                    float? predictiveValue = null;
                    
                    // Predictive property value assignment
                    if (property.serializedObject.targetObject is FlareControl { Type: ControlType.Menu } control)
                    {
                        if (type is PropertyValueType.Float)
                        {
                            var menuInfo = control.MenuItem;
                            var nonZeroTarget = binding.Source is FlarePropertySource.Blendshape ? 100f : 1f;
                        
                            var defaultValue = binder.GetPropertyValue<float>(binding);

                            predictiveValue = defaultValue == 0 ? nonZeroTarget : 0;
                            
                            //if (oppositeTarget >= defaultValue && defaultValue >= 0)
                            //    predictiveValue = defaultValue is 0 ? oppositeTarget : 0f;

                            var predictiveDisable = menuInfo.Type is MenuItemType.Toggle && menuInfo.DefaultState;
                            property.Property(nameof(PropertyInfo.State))
                                .SetValue(predictiveDisable ? ControlState.Disabled : ControlState.Enabled);
                        }
                    }
                    
                    switch (type)
                    {
                        // Seed the default value to start with
                        case PropertyValueType.Float or PropertyValueType.Boolean or PropertyValueType.Integer:
                        {
                            var defaultValue = binder.GetPropertyValue(binding);
                            if (defaultValue is bool boolValue)
                                defaultValue = boolValue ? 1f : 0f;

                            if (defaultValue is Vector4 or Vector3 or Vector2)
                                defaultValue = binder.GetPropertyValue(pseudoProperty!);
                            
                            defaultValue = (float)defaultValue;
                            defaultValue = predictiveValue ?? defaultValue;
                            property.Property(nameof(PropertyInfo.Analog)).SetValue(defaultValue);
                            break;
                        }
                        case PropertyValueType.Vector2 or PropertyValueType.Vector3 or PropertyValueType.Vector4:
                        {
                            var defaultValue = (Vector4)binder.GetPropertyValue(binding);
                            property.Property(nameof(PropertyInfo.Vector)).SetValue(defaultValue);
                            break;
                        }
                        case PropertyValueType.Object:
                        {
                            var defaultValue = (UnityEngine.Object)binder.GetPropertyValue(binding);
                            property.Property(nameof(PropertyInfo.Object)).SetValue(defaultValue);
                            var objectType = binding.GetPseudoProperty(0).Type;
                            property.Property(nameof(PropertyInfo.ObjectType)).SetValue(objectType.AssemblyQualifiedName);
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
                    binding.Type is PropertyValueType.Vector4 ? () => HandlePseudoProperty(3) : null,
                    propertyGroup.SelectionType is not PropertySelectionType.Avatar
                );
            });

            void RefreshList(string search, bool mergeRenderers)
            {
                // TODO: Add optimistic search when typing (no backspace)
                items.Clear();
                if (search == string.Empty)
                {
                    items.AddRange(bindings);
                    list.RefreshItems();
                    return;
                }

                var materialsOnly = search.Contains("t:Material");
                var blendshapesOnly = search.Contains("t:Blendshape");

                search = search
                    .Replace("t:Material", string.Empty)
                    .Replace("t:Blendshape", string.Empty)
                    .Trim();

                var searchParts = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                using (HashSetPool<string>.Get(out var addedRendererParams))
                {
                    var rendererType = typeof(Renderer);
                    
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

                        var contextType = group.ContextType;

                        if (mergeRenderers && rendererType.IsAssignableFrom(contextType))
                        {
                            if (addedRendererParams.Contains(group.Name))
                                continue;
                            else
                                addedRendererParams.Add(group.Name);
                        }

                        items.Add(group);
                    }
                }

                list.RefreshItems();
                list.Query<BindablePropertyCell>().ForEach(BindablePropertyCell.FixSelector);
            }

            searchField.RegisterValueChangedCallback(ctx => RefreshList(ctx.newValue, true));
            //mergeRenderersToggle.RegisterValueChangedCallback(ctx => RefreshList(searchField.value, ctx.newValue));

            VisualElement container = new();
            container.Add(list);
            root.Add(container);
            container.WithGrow(1f);
            container.style.height = new StyleLength(StyleKeyword.Null);
            
            list.selectionType = SelectionType.None;

            var previousProperty = _target?.Property(nameof(PropertyInfo.Name)).stringValue;
            searchField.value = string.IsNullOrWhiteSpace(previousProperty) ? "t:Blendshape " : previousProperty;
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