using UnityEditor;

namespace Flare.Editor.Controllers
{
    [InitializeOnLoad]
    internal class EditorControllerManager
    {
        static EditorControllerManager()
        {
            EditorControllers.Add(new FlareMenuSync());
            EditorControllers.Add(new FlareControlSync());
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < EditorControllers.Controllers.Count; i++)
                EditorControllers.Controllers[i].Update();
        }
    }
}