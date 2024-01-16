using System;
using System.Collections.Generic;

namespace Flare
{
    internal static class EditorControllers
    {
        private static readonly List<IEditorController> _controllers = new();
        
        public static IReadOnlyList<IEditorController> Controllers => _controllers;

        public static T Get<T>(string id) where T : IEditorController
        {
            foreach (var controller in _controllers)
                if (controller is T editorController && controller.Id == id)
                    return editorController;

            throw new InvalidOperationException("Controller is not defined");
        }

        internal static void Add<T>(T controller) where T : IEditorController
        {
            _controllers.Add(controller);
        }
        
        internal interface IEditorController
        {
            string Id { get; }
            
            void Update()
            {
            
            }
        }
    }
}