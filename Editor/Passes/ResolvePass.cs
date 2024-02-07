using System.Linq;
using Flare.Models;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;

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
            var descriptor = context.AvatarDescriptor;
            
            foreach (var module in modules)
            {
                // Feature: Ignore modules that are disabled.
                if (!module.gameObject.activeSelf)
                    continue;
                
                switch (module)
                {
                    case FlareControl control:                
                        // Disqualify controls with objects references not on the avatar.
                        var invalidRefs = control.ObjectToggleCollection.Toggles.Any(t =>
                        {
                            var target = t.GetTargetTransform();
                            if (target == null)
                                return false;

                            return target.GetComponentInParent<VRCAvatarDescriptor>() != descriptor;
                        });

                        if (!invalidRefs)
                        {
                            invalidRefs = control.PropertyGroupCollection.Groups.Any(g =>
                            {
                                var refs = g.SelectionType is PropertySelectionType.Normal
                                    ? g.Inclusions
                                    : g.Exclusions;

                                foreach (var obj in refs)
                                    if (obj != null && obj.GetComponentInParent<VRCAvatarDescriptor>() != descriptor)
                                        return true;

                                return false;
                            });
                        }

                        if (!invalidRefs)
                            flare.AddControl(control);
                        
                        break;
                    case FlareTags tags:
                        flare.AddTags(tags);
                        break;
                }
            }
        }
    }
}