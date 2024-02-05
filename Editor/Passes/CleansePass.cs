using nadena.dev.ndmf;
using UnityEngine;

namespace Flare.Editor.Passes
{
    internal class CleansePass : Pass<CleansePass>
    {
        public override string DisplayName => "Remove Flare Components";

        protected override void Execute(BuildContext context)
        {
            var modules = context.AvatarRootObject.GetComponentsInChildren<FlareModule>(true);
            foreach (var module in modules)
                if (module && module.gameObject)
                    Object.DestroyImmediate(module.gameObject);
        }
    }
}