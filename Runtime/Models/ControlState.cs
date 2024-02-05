using UnityEngine;

namespace Flare.Models
{
    internal enum ControlState
    {
        [InspectorName("When Active")]
        Enabled,
        
        [InspectorName("When Inactive")]
        Disabled
    }
}