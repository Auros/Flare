using Flare.Models;
using nadena.dev.ndmf;
using Sucrose;
using Sucrose.Animation;
using UnityEditor.Animations;
using UnityEngine;
using Random = System.Random;

namespace Flare.Editor.Passes
{
    internal class ControlPass : Pass<ControlPass>
    {
        private readonly Random _random = new();
        
        public override string DisplayName => "Setup Control Properties and Blending";

        protected override void Execute(BuildContext context)
        {
            var flare = context.GetState<FlareAvatarContext>();
            
            if (flare.IsEmpty)
                return;
            
            var frameTimeParameter = flare.GetFrameTimeParameter(context);
            var smoothTree = flare.GetSmoothingBlendTree(context);
            var dbt = flare.GetDirectBlendTree(context);
            var sucrose = flare.GetSucrose(context);

            foreach (var ctx in flare.ControlContexts)
            {
                var id = ctx.Id;
                var parameter = sucrose.NewParameter().WithType(SucroseParameterType.Float).WithName(id);

                var isMenu = ctx.Control.Type is ControlType.Menu;
                var menuType = ctx.Control.MenuItem.Type;
                var menu = ctx.Control.MenuItem;
                
                if (isMenu && menuType is MenuItemType.Toggle)
                    parameter.WithDefaultValue(ctx.Control.MenuItem.DefaultState ? 1f : 0f);
                
                if (isMenu && menuType is MenuItemType.Radial)
                    parameter.WithDefaultValue(ctx.Control.MenuItem.DefaultRadialValue);
                
                // Special check for tagization pass
                if (isMenu && menuType is MenuItemType.Button && menu.IsTagTrigger && !string.IsNullOrWhiteSpace(menu.Tag))
                {
                    // For now, button triggers are exclusively for tags.
                    flare.AddTrigger(menu.Tag, parameter);
                    continue;
                }
                
                var defaultAnimation = sucrose.NewAnimation(builder =>
                {
                    builder.WithName($"{id} (Default)");
                    foreach (var (path, type, name, defaultValue, _) in ctx.Properties)
                        builder.WithBinaryCurve(path, type, name, defaultValue);
                });
                var inverseAnimation = sucrose.NewAnimation(builder =>
                {
                    builder.WithName($"{id} (Inverse)");
                    foreach (var (path, type, name, _, inverseValue) in ctx.Properties)
                        builder.WithBinaryCurve(path, type, name, inverseValue);
                });

                var duration = ctx.Control.Interpolation.Duration;
                
                var toggleBlendTree = dbt.NewChildBlendTree()
                    .WithName(id)
                    .WithParameter(parameter)
                    .NewChildMotion()
                    .WithMotion(defaultAnimation)
                    .BlendTree
                    .NewChildMotion()
                    .WithMotion(inverseAnimation)
                    .BlendTree;
                
                // Non-animated, easy setup! Nothing left to do.
                if (duration is 0)
                    continue;

                var genId = RandomId().ToString();
                
                // Setup linear smoothing
                // I'd like to also support exponential smoothing for end users sometime in the future.

                // Our frame dependent step size is the inverse of the duration (in seconds).
                var stepSize = 1f / duration;
                
                // Create step value (dependent) and step (independent) parameters
                var stepValueParameter = sucrose.NewParameter()
                    .WithName($"{id} (DBT # Step Value - {genId})")
                    .WithType(SucroseParameterType.Float)
                    .WithDefaultValue(stepSize);
                
                var stepParameter = sucrose.NewParameter()
                    .WithName($"{id} (DBT # Step - {genId})")
                    .WithType(SucroseParameterType.Float);

                smoothTree
                    .NewChildBlendTree()
                    .WithParameter(stepValueParameter)
                    .WithType(BlendTreeType.Direct)
                    .WithName($"{id} (DBT # Multiply for Framerate Independence - {genId})")
                    .NewChildBlendTree()
                    .WithType(BlendTreeType.Direct)
                    .NewChildMotion()
                    .WithDirectParameter(frameTimeParameter)
                    .WithMotion(clip =>
                    {
                        clip.WithName($"{id} (DBT # Step * Frame Delta - {genId})");
                        clip.WithBinaryCurve<Animator>(stepParameter.Name, 1f);
                    });
                
                var outputParameter = sucrose.NewParameter()
                    .WithName($"{id} (DBT # Output - {genId})")
                    .WithType(SucroseParameterType.Float);
                
                var ioDeltaParameter = sucrose.NewParameter()
                    .WithName($"{id} (DBT # IO Delta - {genId})")
                    .WithType(SucroseParameterType.Float);
                
                // Assign the output parameter to the toggle blend tree.
                toggleBlendTree.WithParameter(outputParameter);
                
                var ioDeltaLow = sucrose.NewAnimation(builder =>
                {
                    builder.WithName($"{ioDeltaParameter.Name} (Low)");
                    builder.WithBinaryCurve<Animator>(ioDeltaParameter.Name, -1f);
                });
                
                var ioDeltaHigh = sucrose.NewAnimation(builder =>
                {
                    builder.WithName($"{ioDeltaParameter.Name} (High)");
                    builder.WithBinaryCurve<Animator>(ioDeltaParameter.Name, 1f);
                });
                
                // Setup Input, we use the synced parameter.
                smoothTree.NewChildBlendTree()
                    .WithParameter(parameter)
                    .WithName($"Delta = Input ({genId})")
                    .NewChildMotion()
                        .WithThreshold(-1f)
                        .WithMotion(ioDeltaLow)
                        .BlendTree
                    .NewChildMotion()
                        .WithThreshold(1f)
                        .WithMotion(ioDeltaHigh);
                
                smoothTree.NewChildBlendTree()
                    .WithParameter(outputParameter)
                    .WithName($"Delta = -Output ({genId})")
                    .NewChildMotion()
                        .WithThreshold(-1f)
                        .WithMotion(ioDeltaHigh)
                        .BlendTree
                    .NewChildMotion()
                        .WithThreshold(1f)
                        .WithMotion(ioDeltaLow);
                
                smoothTree.NewChildBlendTree()
                    .WithParameter(outputParameter)
                    .WithName($"Output = Output ({genId})")
                    .NewChildMotion()
                        .WithThreshold(-1f)
                        .WithMotion(builder =>
                        {
                            builder.WithName($"{outputParameter.Name} (0)");
                            builder.WithBinaryCurve<Animator>(outputParameter.Name, -1f);
                        })
                        .BlendTree
                    .NewChildMotion()
                        .WithThreshold(1f)
                        .WithMotion(builder =>
                        {
                            builder.WithName($"{outputParameter.Name} (1)");
                            builder.WithBinaryCurve<Animator>(outputParameter.Name, 1f);
                        });
                
                smoothTree.NewChildBlendTree()
                    .WithDirectParameter(stepParameter)
                    .WithParameter(ioDeltaParameter)    
                    .WithName($"Linear Blend ({genId})")
                    .NewChildMotion()
                        .WithThreshold(-0.1f)
                        .WithMotion(builder =>
                        {
                            builder.WithName($"{outputParameter.Name} (-1)");
                            builder.WithBinaryCurve<Animator>(outputParameter.Name, -1f);
                        })
                        .BlendTree
                    .NewChildMotion()
                        .WithThreshold(0f)
                        .WithMotion(builder =>
                        {
                            builder.WithName($"{outputParameter.Name} (0)");
                            builder.WithBinaryCurve<Animator>(outputParameter.Name, 0f);
                        })
                        .BlendTree
                    .NewChildMotion()
                        .WithThreshold(0.1f)
                        .WithMotion(builder =>
                        {
                            builder.WithName($"{outputParameter.Name} (1)");
                            builder.WithBinaryCurve<Animator>(outputParameter.Name, 1f);
                        });
            }
        }

        private int RandomId()
        {
            // Chance of collision is zero from earlier pass. Just doing this cause why not.
            return _random.Next(100_000, 1_000_000);
        }
    }
}