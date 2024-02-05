using Flare.Editor.Animation;
using Flare.Editor.Extensions;
using Flare.Editor.Passes;
using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(Flare.Editor.FlarePlugin))]

namespace Flare.Editor
{
    public class FlarePlugin : Plugin<FlarePlugin>
    {
        public override string DisplayName => nameof(Flare);

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).Run<ResolvePass>();
            
            // Clone animators (using the Modular Avatar implementation... ty bd_ <3)
            InPhase(BuildPhase.Generating).Run("Clone Animators (MA Impl)", AnimationUtilities.CloneAllControllers);
            InPhase(BuildPhase.Generating).Run<MenuGenerationPass>();
            InPhase(BuildPhase.Generating).Run<ContainerizationPass>();
            InPhase(BuildPhase.Generating).Run<ParameterGenerationPass>();
            
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run<ControlPass>()
                .Then.Run<ParametrizationPass>()
                .Then.Run<MenuizationPass>()
                .Then.Run<TagizationPass>();
        }
    }
}
