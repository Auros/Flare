using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Pool;

namespace Flare.Editor.Passes
{
    /// <summary>
    /// This pass sets up the FlareAvatarContext and provides the initial values.
    /// </summary>
    internal class ResolvePass : Pass<ResolvePass>
    {
        public override string DisplayName => "Collect Flare Modules";

        protected override void Execute(BuildContext context)
        {
            var root = context.AvatarRootObject;
            var flare = context.GetState<FlareAvatarContext>();
            var modules = root.GetComponentsInChildren<FlareModule>();

            foreach (var module in modules)
            {
                // Feature: Ignore modules that are disabled.
                if (!module.gameObject.activeSelf)
                    continue;
                
                switch (module)
                {
                    case FlareControl control:                
                        // Disqualify controls with objects references not on the avatar or other errors.
                        // We use the error reporter designed for the UI.
                        var errors = ListPool<Object?>.Get();
                        
                        control.GetReferencesNotOnAvatar(errors);
                        var errorCount = errors.Count;
                        ListPool<Object?>.Release(errors);
                        
                        if (errorCount is 0)
                            flare.AddControl(control);
                        
                        break;
                    
                    case FlareTags tags:
                        flare.AddTags(tags);
                        break;
                    
                    case FlareSettings settings:
                        flare.PreferredWriteDefaults = settings.WriteDefaults;
                        break;
                }
            }
        }
    }
}