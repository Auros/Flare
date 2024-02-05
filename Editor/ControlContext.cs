using System.Collections.Generic;
using Flare.Editor.Models;
using Flare.Models;
using JetBrains.Annotations;

namespace Flare.Editor
{
    [PublicAPI]
    internal class ControlContext
    {
        private readonly List<AnimatableBinaryProperty> _properties = new();
        
        public string Id { get; }

        public bool IsBinary => Control.MenuItem.Type is MenuItemType.Toggle or MenuItemType.Button;
        
        public FlareControl Control { get; }

        public IReadOnlyList<AnimatableBinaryProperty> Properties => _properties;
        
        public ControlContext(string id, FlareControl control)
        {
            Id = id;
            Control = control;
        }

        public void AddProperty(AnimatableBinaryProperty property)
        {
            _properties.Add(property);
        }
    }
}