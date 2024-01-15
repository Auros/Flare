using Flare.Editor.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Extensions
{
    /// <summary>
    /// Builder-syntax for UI toolkit items for a more declarative UI building style
    /// </summary>
    internal static class UIToolkitBuilderExtensions
    {
        public static HorizontalSpacer CreateHorizontalSpacer(this VisualElement element, float height = 8f)
        {
            HorizontalSpacer spacer = new(height);
            element.Add(spacer);
            return spacer;
        }
        
        public static VisualElement CreateHorizontal(this VisualElement element)
        {
            VisualElement child = new()
            {
                style = { flexDirection = FlexDirection.Row }
            };
            element.Add(child);
            return child;
        }
        
        public static PropertyField CreatePropertyField(this VisualElement element, SerializedProperty property)
        {
            PropertyField propertyField = new(property);
            element.Add(propertyField);
            return propertyField;
        }
        
        public static Label CreateLabel(this VisualElement element, string text = "")
        {
            Label label = new(text);
            element.Add(label);
            return label;
        }
        
        public static Slider CreateSlider(this VisualElement element)
        {
            Slider slider = new()
            {
                lowValue = 0f,
                highValue = 1f
            };
            element.Add(slider);
            return slider;
        }

        public static Slider WithStart(this Slider slider, float start = 0.0f)
        {
            slider.lowValue = start;
            return slider;
        }

        public static Slider WithEnd(this Slider slider, float end = 1.0f)
        {
            slider.highValue = end;
            return slider;
        }

        public static T WithLabel<T>(this T element, string label = "") where T : Slider
        {
            element.label = label;
            return element;
        }

        public static T WithLabel<T, TValueType>(this T element, string label = "") where T : BaseField<TValueType>
        {
            element.label = label;
            return element;
        }

        public static T WithFontStyle<T>(this T element, FontStyle fontStyle) where T : VisualElement
        {
            element.style.unityFontStyleAndWeight = fontStyle;
            return element;
        }

        public static T BindTo<T>(this T element, string path) where T : IBindable
        {
            element.bindingPath = path;
            return element;
        }
        
        public static T BindTo<T>(this T element, SerializedProperty property) where T : IBindable
        {
            element.bindingPath = property.propertyPath;
            return element;
        }

        public static T WithPadding<T>(this T element, float padding) where T : VisualElement
        {
            element.style.paddingTop = padding;
            element.style.paddingLeft = padding;
            element.style.paddingRight = padding;
            element.style.paddingBottom = padding;
            return element;
        }

        public static T WithRadius<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            return element;
        }

        public static T WithMargin<T>(this T element, float margin) where T : VisualElement
        {
            element.style.marginTop = margin;
            element.style.marginLeft = margin;
            element.style.marginRight = margin;
            element.style.marginBottom = margin;
            return element;
        }
    }
}