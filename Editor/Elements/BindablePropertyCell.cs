using System;
using Flare.Editor.Extensions;
using Flare.Editor.Models;
using Flare.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flare.Editor.Elements
{
    internal class BindablePropertyCell : VisualElement
    {
        private static Texture2D? _paneOptionsImage;
        
        private readonly Label _propertyNameLabel;
        private readonly Label _propertyContextLabel;

        private Action? _onSelect;
        //private readonly Label _propertyTypeText;
        private readonly TextField _textField;
        private readonly ColorField _colorField;
        
        private readonly Button _selectButton;

        private Action? _onJump;
        private Color _currentColorValue;
        
        private bool _isColor;
        private Action? _onVectorX;
        private Action? _onVectorY;
        private Action? _onVectorZ;
        private Action? _onVectorW;
        
        public BindablePropertyCell()
        {
            if (!_paneOptionsImage)
                _paneOptionsImage = (Texture2D)EditorGUIUtility.Load(GetPaneOptionsImage());
            
            this.WithMargin(5f);

            VisualElement leftSide = new();
            VisualElement rightSide = new();
            var inner = this.CreateHorizontal();
            var rightTop = rightSide.CreateHorizontal();
            VisualElement rightBottom = new();
            rightSide.Add(rightBottom);
            
            rightBottom.style.flexGrow = 1f;
            rightTop.style.flexShrink = 1f;
            
            inner
                .WithBackgroundColor(new Color32(0x46, 0x46, 0x46, 0xFF))
                .WithBorderColor(new Color32(0x1A, 0x1A, 0x1A, 0xFF))
                .WithBorderRadius(3f)
                .WithBorderWidth(1f)
                .WithPadding(5f);

            leftSide.style.flexGrow = 1f;
            rightTop.style.flexShrink = 1f;
            
            _propertyNameLabel = leftSide.CreateLabel();
            _propertyContextLabel = leftSide.CreateFlareLabel().WithFontSize(8f);

            //_propertyTypeText = rightTop.CreateLabel();
            _textField = new TextField();
            _textField.SetEnabled(false);
            _textField.style.width = 115f;
            _textField.Q<TextElement>().WithTextAlign(TextAnchor.MiddleRight).WithFontSize(9f);

            _colorField = new ColorField();
            _colorField.RegisterValueChangedCallback(_ =>
            {
                _colorField.SetValueWithoutNotify(_currentColorValue);
            });
            _colorField.style.width = 115f;
            _colorField.showEyeDropper = false;
            
            rightTop.Add(_colorField);
            rightTop.Add(_textField);
            
            _selectButton = new Button
            {
                text = "Set",
                style = { width = 40f }
            };
            VisualElement menu = new()
            {
                style =
                {
                    backgroundImage = _paneOptionsImage,
                    width = 10f,
                    height = 20f
                }
            };
            rightTop.Add(_selectButton);
            rightTop.Add(menu);
            var contextualMenuManipulator = new ContextualMenuManipulator(ModifyContextMenu);
            contextualMenuManipulator.activators.Add(new ManipulatorActivationFilter()
            {
                button = MouseButton.LeftMouse
            });
            
            menu.AddManipulator(contextualMenuManipulator);

            inner.Add(leftSide);
            inner.Add(rightSide);
            
            InitializeSelctorFix();
        }

        // ReSharper disable once InvertIf
        private void ModifyContextMenu(ContextualMenuPopulateEvent ctx)
        {
            ctx.menu.AppendAction("Jump To Source", _ => _onJump?.Invoke());

            if (_onVectorX is not null)
            {
                var channel = _isColor ? "R" : "X";
                ctx.menu.AppendAction($"Use '{channel}' property", _ => _onVectorX?.Invoke());
            }
            if (_onVectorY is not null)
            {
                var channel = _isColor ? "G" : "Y";
                ctx.menu.AppendAction($"Use '{channel}' property", _ => _onVectorY?.Invoke());
            }
            if (_onVectorZ is not null)
            {
                var channel = _isColor ? "B" : "Z";
                ctx.menu.AppendAction($"Use '{channel}' property", _ => _onVectorZ?.Invoke());
            }
            if (_onVectorW is not null)
            {
                var channel = _isColor ? "A" : "W";
                ctx.menu.AppendAction($"Use '{channel}' property", _ => _onVectorW?.Invoke());
            }
        }

        public void SetData(FlareProperty property, object currentValue, Action onSelect, Action? onJump,
            Action? onVectorX, Action? onVectorY, Action? onVectorZ, Action? onVectorW)
        {
            _propertyNameLabel.text = $"<b>{property.Name}</b>";
            _propertyContextLabel.text = $"{property.ContextType.Name} ({property.Path})";
            _propertyContextLabel.tooltip = property.Path;

            var valueString = currentValue.ToString();
            _textField.value = valueString;

            _selectButton.clicked -= _onSelect;
            _selectButton.clicked += onSelect;
            _onSelect = onSelect;

            _isColor = property.Color is not PropertyColorType.None;
            if (_isColor)
            {
                _colorField.hdr = property.Color == PropertyColorType.HDR;
                _colorField.showAlpha = property.Type == PropertyValueType.Vector4;
                _currentColorValue = currentValue switch
                {
                    Vector4 vector4 => vector4,
                    Vector3 vector3 => (Vector4)vector3,
                    _ => _colorField.value
                };
                _colorField.value = _currentColorValue;
            }

            var isVector = property.Type
                is PropertyValueType.Vector2
                or PropertyValueType.Vector3
                or PropertyValueType.Vector4;

            _onVectorX = onVectorX;
            _onVectorY = onVectorY;
            _onVectorZ = onVectorZ;
            _onVectorW = onVectorW;
            
            _textField.Q<TextElement>().WithFontSize(isVector ? 9f : 12f);
            _textField.style.display = _isColor ? DisplayStyle.None : DisplayStyle.Flex;
            _colorField.style.display = _isColor ? DisplayStyle.Flex : DisplayStyle.None;
            
            _onJump = onJump;
        }

        // Removes the hover selection on the individual cells
        private void InitializeSelctorFix()
        {
            void SelectionFix(GeometryChangedEvent ctx)
            {
                if (ctx.target is not VisualElement e)
                    return;
                    
                FixSelector(e);
                UnregisterCallback<GeometryChangedEvent>(SelectionFix);
            }
            RegisterCallback<GeometryChangedEvent>(SelectionFix);
        }

        public static void FixSelector(VisualElement cell)
        {
            cell.RemoveFromClassList("unity-collection-view__item");
        }

        private static string GetPaneOptionsImage()
        {
            // There might be a better way to do this, but this is the simplest for me right now as I can
            // only find examples on using the pane options icon via USS.
            var skin = EditorGUIUtility.isProSkin ? "DarkSkin" : "LightSkin";
            return $"Builtin Skins/{skin}/Images/pane options.png";
        }
    }
}