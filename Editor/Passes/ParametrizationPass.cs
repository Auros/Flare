using System;
using System.Linq;
using Flare.Models;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Flare.Editor.Passes
{
    internal class ParametrizationPass : Pass<ParametrizationPass>
    {
        public override string DisplayName => "Control and Tag Expression Parametizer";

        protected override void Execute(BuildContext context)
        {
            var flare = context.GetState<FlareAvatarContext>();
            var descriptor = context.AvatarDescriptor;
            var vrcParams = descriptor.expressionParameters;

            var flareParameters = flare.ControlContexts.Select(c =>
            {
                var defaultValue = c.Control.Type is ControlType.Menu ? c.Control.MenuItem.Type switch
                {
                    MenuItemType.Button => 0f,
                    MenuItemType.Toggle => c.Control.MenuItem.DefaultState ? 1f : 0f,
                    MenuItemType.Radial => c.Control.MenuItem.DefaultRadialValue,
                    _ => throw new ArgumentOutOfRangeException()
                } : 0f;
                
                return new VRCExpressionParameters.Parameter
                {
                    name = c.Id,
                    saved = true,
                    networkSynced = true,
                    defaultValue = defaultValue,
                    valueType = c.IsBinary ? VRCExpressionParameters.ValueType.Bool : VRCExpressionParameters.ValueType.Float
                };
            });

            vrcParams.parameters = vrcParams.parameters.Concat(flareParameters).ToArray();
        }
    }
}