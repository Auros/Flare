using UnityEditor;
using UnityEngine;

namespace Flare.Editor
{
    internal static class FlareUI
    {
        public static readonly Color BorderColor = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
        
        public static readonly Color BackgroundColor = EditorGUIUtility.isProSkin
            ? new Color32(0x46, 0x46, 0x46, 0xFF) :  new Color32(0xDE, 0xDE, 0xDE, 0xFF);
        
        public static readonly Color EnabledColor = EditorGUIUtility.isProSkin
            ? new Color32(0x5B, 0xDD, 0x55, 0xFF) : new Color32(0x00, 0x7C, 0x02, 0xFF);
        
        public static readonly Color DisabledColor = EditorGUIUtility.isProSkin
            ? new Color32(0xFF, 0x7C, 0x7C, 0xFF) : new Color32(0x93, 0x00, 0x00, 0xFF);
    }
}