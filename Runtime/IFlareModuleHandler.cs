namespace Flare
{
    internal interface IFlareModuleHandler<in T> : EditorControllers.IEditorController
    {
        void Add(T module);

        void Remove(T module);
    }
}