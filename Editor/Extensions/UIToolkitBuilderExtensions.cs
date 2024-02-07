﻿using System;
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
        public static VisualElement CreateElement(this VisualElement element)
        {
            VisualElement child = new();
            element.Add(child);
            return child;
        }
        
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

        public static Button CreateButton(this VisualElement element, string text, Action? action = null)
        {
            Button button = new(action) { text = text };
            button.WithHeight(20f);
            element.Add(button);
            return button;
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
        
        public static Label CreateFlareLabel(this VisualElement element, string text = "")
        {
            FlareLabel label = new(text);
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

        public static EnumField CreateEnumField(this VisualElement element, Enum value)
        {
            EnumField field = new(value);
            element.Add(field);
            return field;
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

        public static PropertyField WithLabel(this PropertyField field, string label)
        {
            field.label = label;
            return field;
        }

        public static PropertyField WithTooltip(this PropertyField field, string tooltip)
        {
            field.tooltip = tooltip;
            return field;
        }

        public static T WithFontStyle<T>(this T element, FontStyle fontStyle) where T : VisualElement
        {
            element.style.unityFontStyleAndWeight = fontStyle;
            return element;
        }

        public static T WithFontSize<T>(this T element, float fontSize) where T : VisualElement
        {
            element.style.fontSize = fontSize;
            return element;
        }

        public static T BindTo<T>(this T element, string path) where T : IBindable
        {
            element.bindingPath = path;
            return element;
        }
        
        public static T BindTo<T>(this T element, SerializedProperty? property) where T : IBindable
        {
            element.bindingPath = property?.propertyPath;
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

        public static T WithBorderRadius<T>(this T element, float radius) where T : VisualElement
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

        public static T WithMarginTop<T>(this T element, float marginTop) where T : VisualElement
        {
            element.style.marginTop = marginTop;
            return element;
        }
        
        public static T WithMarginLeft<T>(this T element, float marginLeft) where T : VisualElement
        {
            element.style.marginLeft = marginLeft;
            return element;
        }
        
        public static T WithWidth<T>(this T element, float width) where T : VisualElement
        {
            element.style.width = width;
            return element;
        }
        
        public static T WithGrow<T>(this T element, float grow) where T : VisualElement
        {
            element.style.flexGrow = grow;
            return element;
        }
        
        public static T WithShrink<T>(this T element, float shrink) where T : VisualElement
        {
            element.style.flexGrow = shrink;
            return element;
        }
        
        public static T WithWrap<T>(this T element, Wrap wrap) where T : VisualElement
        {
            element.style.flexWrap = wrap;
            return element;
        }

        public static T WithBorderWidth<T>(this T element, float width) where T : VisualElement
        {
            element.style.borderTopWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            return element;
        }

        public static T WithBorderColor<T>(this T element, Color color) where T : VisualElement
        {
            element.style.borderTopColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            return element;
        }

        public static T WithBackgroundColor<T>(this T element, Color color) where T : VisualElement
        {
            element.style.backgroundColor = color;
            return element;
        }
        
        public static T WithColor<T>(this T element, Color color) where T : VisualElement
        {
            element.style.color = color;
            return element;
        }
        
        public static T WithHeight<T>(this T element, float height) where T : VisualElement
        {
            element.style.height = height;
            return element;
        }
        
        public static T WithMinHeight<T>(this T element, float minHeight) where T : VisualElement
        {
            element.style.minHeight = minHeight;
            return element;
        }
        
        public static T WithMaxHeight<T>(this T element, float maxHeight) where T : VisualElement
        {
            element.style.maxHeight = maxHeight;
            return element;
        }

        public static T WithTextAlign<T>(this T element, TextAnchor align) where T : VisualElement
        {
            element.style.unityTextAlign = align;
            return element;
        }

        public static T WithName<T>(this T element, string name) where T : VisualElement
        {
            element.name = name;
            return element;
        }
        
        public static T Visible<T>(this T element, bool visible) where T : VisualElement
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            return element;
        }
        
        public static T Enabled<T>(this T element, bool enabled) where T : VisualElement
        {
            element.SetEnabled(enabled);
            return element;
        }
    }
}