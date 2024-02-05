using System;
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
        
        public ControlContext(FlareControl control)
        {
            Control = control;

            if (control.Type is not ControlType.Menu || string.IsNullOrWhiteSpace(control.MenuItem.Name))
                Id = Guid.NewGuid().ToString();
            else
                Id = control.MenuItem.Name;

            Id = $"[Flare] {Id}";
        }

        public void AddProperty(AnimatableBinaryProperty property)
        {
            _properties.Add(property);
        }
    }
}