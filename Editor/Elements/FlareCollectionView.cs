using System;
using Flare.Editor.Extensions;
using Flare.Editor.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Flare.Editor.Elements
{
    public class FlareCollectionView<T> : VisualElement, IFlareBindable where T : VisualElement, IFlareBindable
    {
        private readonly Func<T> _builder;
        private readonly VisualElement _container;
        
        private Action<T, int>? _binder;
        private SerializedProperty? _arrayProperty;
        
        public FlareCollectionView(
            Func<T> builder,
            Action<T, int>? binder = null,
            SerializedProperty? arrayProperty = null)
        {
            _binder = binder;
            _builder = builder;
            _arrayProperty = arrayProperty;
            _container = this.CreateElement();
            
            if (arrayProperty is not null)
                SetBinding(arrayProperty);
        }

        private void BuildCollectionView()
        {
            if (_arrayProperty == null)
                return;
            
            var index = 0;
            var endProperty = _arrayProperty.GetEndProperty();

            // Copy the array property to iterate over it.
            var property = _arrayProperty.Copy();

            // Look at the next property.
            property.NextVisible(true);

            do // If I had a dollar for every time I used a do while loop in a project, I would have $1
            {
                // Stop looking after the end property
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;
                
                // Ignore array size properties.
                if (property.propertyType is SerializedPropertyType.ArraySize)
                    continue;

                T element;
                if (index >= _container.childCount)
                {
                    // Add new elements as the array has grown
                    element = _builder.Invoke();
                    _container.Add(element);
                }
                else
                {
                    // We don't need to resize for this property.
                    element = (T)_container[index];
                }

                var targetIndex = index;
                _binder?.Invoke(element, targetIndex);
                element.SetBinding(property);
                index++;
            }
            while (property.NextVisible(false));

            // Remove any extra elements.
            while (_container.childCount > index)
                _container.RemoveAt(_container.childCount - 1);
        }

        public void SetData(Action<T, int> binder)
        {
            _binder = binder;
        }
        
        /// <summary>
        /// Bind to this collection view.
        /// </summary>
        /// <param name="property">The array property</param>
        public void SetBinding(SerializedProperty property)
        {
            _arrayProperty = property.Copy();
            var arraySizeProperty = _arrayProperty.Field("size");

            this.Unbind();
            this.TrackPropertyValue(arraySizeProperty, _ => BuildCollectionView());
            BuildCollectionView();
        }
    }
}