namespace UnityEditor
{
    public static class EditorExtensions
    {
        public static SerializedProperty? Field(this SerializedObject serializedObject, string fieldName)
        {
            return serializedObject.FindProperty(fieldName);
        }

        public static SerializedProperty? Property(this SerializedObject serializedObject, string propertyName)
        {
            return serializedObject.FindProperty($"<{propertyName}>k__BackingField");
        }
        
        public static SerializedProperty? Field(this SerializedProperty serializedObject, string fieldName)
        {
            return serializedObject.FindPropertyRelative(fieldName);
        }

        public static SerializedProperty Property(this SerializedProperty serializedObject, string propertyName)
        {
            return serializedObject.FindPropertyRelative($"<{propertyName}>k__BackingField");
        }
    }
}