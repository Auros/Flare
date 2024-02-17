using System;
using Flare.Editor.Editor.Extensions;
using Flare.Editor.Extensions;
using Flare.Editor.Models;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDKBase;

namespace Flare.Editor.Elements
{
    /// <summary>
    /// Like a <see cref="PropertyField"/>, but scoped to <see cref="PropertyValueType"/>
    /// </summary>
    internal class CombinatoryFieldElement : VisualElement, IFlareBindable
    {
        private readonly Slider _slider;
        private readonly Toggle _toggle;
        private readonly ColorField _colorField;
        private readonly FloatField _floatField;
        private readonly IntegerField _integerField;
        private readonly Vector2Field _vector2Field;
        private readonly Vector3Field _vector3Field;
        private readonly Vector4Field _vector4Field;

        private readonly VisualElement[] _fields;

        private EventCallback<ChangeEvent<int>>? _intChange;
        private EventCallback<ChangeEvent<bool>>? _boolChange;
        private EventCallback<ChangeEvent<float>>? _floatChange;
        private EventCallback<ChangeEvent<Color>>? _colorChange;
        private EventCallback<ChangeEvent<Vector2>>? _vector2Change;
        private EventCallback<ChangeEvent<Vector3>>? _vector3Change;
        private EventCallback<ChangeEvent<Vector4>>? _vector4Change;
        private bool _useOverrideValue;
        
        public CombinatoryFieldElement()
        {
            _slider = new Slider();
            _toggle = new Toggle();
            _colorField = new ColorField();
            _floatField = new FloatField();
            _integerField = new IntegerField();
            _vector2Field = new Vector2Field();
            _vector3Field = new Vector3Field();
            _vector4Field = new Vector4Field();

            Add(_toggle);
            Add(_colorField);
            Add(_floatField);
            Add(_integerField);
            Add(_vector2Field);
            Add(_vector3Field);
            Add(_vector4Field);
            
            Add(_slider);
            _slider.WithStart().WithEnd();
            _slider.style.marginBottom = 8f;

            _fields = new VisualElement[]
            {
                _slider,
                _toggle,
                _colorField,
                _floatField,
                _integerField,
                _vector2Field,
                _vector3Field,
                _vector4Field
            };

            foreach (var visualElement in _fields)
            {
                visualElement.AddToClassList(ObjectField.alignedFieldUssClassName);
                visualElement.Visible(false);
            }
        }

        public void SetMode(bool useOverride)
        {
            _useOverrideValue = useOverride;
        }

        public void SetLabel(string label)
        {
            _toggle.label = label;
            _colorField.label = label;
            _floatField.label = label;
            _integerField.label = label;
            _vector2Field.label = label;
            _vector3Field.label = label;
            _vector4Field.label = label;
        }
        
        /// <summary>
        /// Bind to this <see cref="CombinatoryFieldElement"/>
        /// </summary>
        /// <param name="property">This property should correspond to <see cref="PropertyInfo"/></param>
        public void SetBinding(SerializedProperty property)
        {
            this.Unbind();
            this.TrackPropertyValue(property.Property(nameof(PropertyInfo.Name)), _ => UpdateElement(property));
            this.TrackPropertyValue(property.Property(nameof(PropertyInfo.Path)), _ => UpdateElement(property));
            UpdateElement(property);
        }

        private void UpdateElement(SerializedProperty property)
        {
            var colorType = (PropertyColorType)property.Property(nameof(PropertyInfo.ColorType)).enumValueIndex;
            var valueType = (PropertyValueType)property.Property(nameof(PropertyInfo.ValueType)).enumValueIndex;
            var isColor = colorType is not PropertyColorType.None;
            
            VisualElement target = valueType switch
            {
                PropertyValueType.Boolean => _toggle,
                PropertyValueType.Integer => _integerField,
                PropertyValueType.Float => _floatField,
                PropertyValueType.Vector2 => _vector2Field,
                PropertyValueType.Vector3 => isColor ? _colorField : _vector3Field,
                PropertyValueType.Vector4 => isColor ? _colorField : _vector4Field,
                _ => throw new ArgumentOutOfRangeException()
            };

            foreach (var element in _fields)
            {
                element.Unbind();
                element.Visible(target == element);
            }
            _slider.SetEnabled(false);

            var nameProperty = property.Property(nameof(PropertyInfo.Name));
            var pathProperty = property.Property(nameof(PropertyInfo.Path));
            var analogProperty = property.Property(nameof(PropertyInfo.Analog));
            var vectorProperty = property.Property(nameof(PropertyInfo.Vector));

            var overrideAnalogProperty = property.Property(nameof(PropertyInfo.OverrideDefaultAnalog));
            var overrideVectorProperty = property.Property(nameof(PropertyInfo.OverrideDefaultVector));

            var propertyName = nameProperty.stringValue;
            var propertyPath = pathProperty.stringValue;
            
            var avatarDescriptor = (property.serializedObject.targetObject as FlareControl)!
                .GetComponentInParent<VRC_AvatarDescriptor>();
            
            float? defaultAnalogValue = null;
            Vector4? defaultVectorValue = null;

            var typeName = property.Property(nameof(PropertyInfo.ContextType)).stringValue;
            var contextType = Type.GetType(typeName);

            if (avatarDescriptor && contextType != null)
            {
                var root = avatarDescriptor.gameObject;
                switch (valueType)
                {
                    case PropertyValueType.Integer:
                    {
                        var binding = EditorCurveBinding.DiscreteCurve(propertyPath, contextType, propertyName);
                        if (AnimationUtility.GetDiscreteIntValue(root, binding, out var intValue))
                            defaultAnalogValue = intValue;
                        break;
                    }
                    case PropertyValueType.Float or PropertyValueType.Boolean:
                    {
                        var binding = EditorCurveBinding.FloatCurve(propertyPath, contextType, propertyName);
                        if (AnimationUtility.GetFloatValue(root, binding, out var floatValue))
                            defaultAnalogValue = floatValue;
                        break;
                    }
                    default:
                    {
                        // Must be a vector type :aware: (In xs, ys, etc. the "s" stands for suffix)
                        var xs = colorType is PropertyColorType.RGB ? "r" : "x";
                        var ys = colorType is PropertyColorType.RGB ? "g" : "y";
                        var zs = colorType is PropertyColorType.RGB ? "b" : "z";
                        var ws = colorType is PropertyColorType.RGB ? "a" : "w";
                    
                        var x = EditorCurveBinding.FloatCurve(propertyPath, contextType, $"{propertyName}.{xs}");
                        var y = EditorCurveBinding.FloatCurve(propertyPath, contextType, $"{propertyName}.{ys}");
                        var z = EditorCurveBinding.FloatCurve(propertyPath, contextType, $"{propertyName}.{zs}");
                        var w = EditorCurveBinding.FloatCurve(propertyPath, contextType, $"{propertyName}.{ws}");
                        
                        var xExists = AnimationUtility.GetFloatValue(root, x, out var xValue);
                        var yExists = AnimationUtility.GetFloatValue(root, y, out var yValue);
                        
                        // If we're missing x or y, this isn't a vector.
                        if (xExists && yExists)
                        {
                            _ = AnimationUtility.GetFloatValue(root, z, out var zValue);
                            _ = AnimationUtility.GetFloatValue(root, w, out var wValue);
                            defaultVectorValue = new Vector4(xValue, yValue, zValue, wValue);
                        }
                        
                        break;
                    }
                }
            }
            
            // ReSharper disable once ConvertToLocalFunction
            Func<SerializedProperty, float> getAnalog = prop =>
                enabledInHierarchy ? prop.floatValue : defaultAnalogValue.GetValueOrDefault();

            // ReSharper disable once ConvertToLocalFunction
            Func<SerializedProperty, Vector4> getVector = prop =>
                enabledInHierarchy ? prop.vector4Value : defaultVectorValue.GetValueOrDefault();

            var mainAnalogProperty = _useOverrideValue ? overrideAnalogProperty : analogProperty;
            var mainVectorProperty = _useOverrideValue ? overrideVectorProperty : vectorProperty;
            
            switch (target)
            {
                case Toggle toggle:
                    toggle.UnregisterValueChangedCallback(_boolChange);
                    _boolChange = evt => mainAnalogProperty.SetValue(evt.newValue ? 1f : 0f);
                    toggle.RegisterValueChangedCallback(_boolChange);
                    toggle.TrackPropertyValue(mainAnalogProperty, prop => toggle.value = getAnalog(prop) is not 0);
                    toggle.value = getAnalog(mainAnalogProperty) is not 0;
                    break;
                case IntegerField integerField:
                    integerField.UnregisterValueChangedCallback(_intChange);
                    _intChange = evt => mainAnalogProperty.SetValue((float)evt.newValue);
                    integerField.RegisterValueChangedCallback(_intChange);
                    integerField.TrackPropertyValue(mainAnalogProperty, prop => integerField.value = (int)getAnalog(prop));
                    integerField.value = (int)getAnalog(mainAnalogProperty);
                    break;
                case FloatField floatField:
                    floatField.UnregisterValueChangedCallback(_floatChange);
                    _floatChange = evt => mainAnalogProperty.SetValue(evt.newValue);

                    var isBlendShape = nameProperty.stringValue.StartsWith("blendShape.");
                    var maxSliderValue = isBlendShape ? 100f : 1f;
                    _slider.highValue = maxSliderValue;
                    
                    floatField.TrackPropertyValue(mainAnalogProperty, prop =>
                    {
                        var defaultFloatValue = getAnalog(prop);
                        floatField.value = defaultFloatValue;

                        var shouldBeEnabled = defaultFloatValue >= 0 && defaultFloatValue <= maxSliderValue && enabledSelf;
                        _slider.SetEnabled(shouldBeEnabled);
                        _slider.Visible(shouldBeEnabled);
                        _slider.value = defaultFloatValue;
                    });
                    var defaultValue = getAnalog(mainAnalogProperty);
                    floatField.value = defaultValue;
                    floatField.RegisterValueChangedCallback(_floatChange);
                    
                    var shouldBeEnabled = defaultValue >= 0 && defaultValue <= maxSliderValue && enabledSelf;
                    _slider.SetEnabled(shouldBeEnabled);
                    _slider.Visible(shouldBeEnabled);
                    _slider.value = defaultValue;
                    _slider.RegisterValueChangedCallback(evt =>
                    {
                        var should = evt.newValue >= 0 && evt.newValue <= maxSliderValue && enabledSelf;
                        _slider.Visible(should);

                        if (!should)
                            return;

                        _floatChange.Invoke(evt);
                    });
                    break;
                case Vector2Field v2Field:
                    v2Field.UnregisterValueChangedCallback(_vector2Change);
                    _vector2Change = evt => mainVectorProperty.SetValue((Vector4)evt.newValue);
                    v2Field.RegisterValueChangedCallback(_vector2Change);
                    v2Field.TrackPropertyValue(mainVectorProperty, prop => v2Field.value = getVector(prop));
                    v2Field.value = getVector(mainVectorProperty);
                    break;
                case Vector3Field v3Field:
                    v3Field.UnregisterValueChangedCallback(_vector3Change);
                    _vector3Change = evt => mainVectorProperty.SetValue((Vector4)evt.newValue);
                    v3Field.RegisterValueChangedCallback(_vector3Change);
                    v3Field.TrackPropertyValue(mainVectorProperty, prop => v3Field.value = getVector(prop));
                    v3Field.value = getVector(mainVectorProperty);
                    break;
                case Vector4Field v4Field:
                    v4Field.UnregisterValueChangedCallback(_vector4Change);
                    _vector4Change = evt => mainVectorProperty.SetValue(evt.newValue);
                    v4Field.RegisterValueChangedCallback(_vector4Change);
                    v4Field.TrackPropertyValue(mainVectorProperty, prop => v4Field.value = getVector(prop));
                    v4Field.value = getVector(mainVectorProperty);
                    break;
                case ColorField colorField:
                    colorField.UnregisterValueChangedCallback(_colorChange);
                    _colorChange = evt => mainVectorProperty.SetValue((Vector4)evt.newValue);
                    colorField.RegisterValueChangedCallback(_colorChange);
                    colorField.TrackPropertyValue(mainVectorProperty, prop => colorField.value = getVector(prop));
                    colorField.showAlpha = valueType is PropertyValueType.Vector4;
                    colorField.hdr = colorType is PropertyColorType.HDR;
                    colorField.showEyeDropper = enabledSelf;
                    colorField.value = getVector(mainVectorProperty);
                    break;
            }
            
            if (propertyName == string.Empty)
                foreach (var field in _fields)
                    field.Visible(false);

            target.MarkDirtyRepaint();
        }
    }
}