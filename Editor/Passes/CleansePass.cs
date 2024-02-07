using System.Linq;
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
            {
                if (!module || !module.gameObject)
                    continue;

                // Make sure we don't delete a GameObject with stuff on it
                var deleteSelf = module.gameObject.GetComponentsInChildren<Component>()
                    .Any(c => c is not Transform && c is not FlareModule);
                
                Object.DestroyImmediate(deleteSelf ? module : module.gameObject);
            }
        }
    }
}