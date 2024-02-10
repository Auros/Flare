using System;
using System.Linq;
using Flare.Models;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
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
            
            if (flare.IsEmpty)
                return;

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
                    networkSynced = true,
                    defaultValue = defaultValue,
                    saved = c.Control.Type is ControlType.Menu && c.Control.MenuItem.IsSaved,
                    valueType = c.IsBinary ? VRCExpressionParameters.ValueType.Bool : VRCExpressionParameters.ValueType.Float
                };
            });

            if (vrcParams == null)
            {
                vrcParams = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                vrcParams.parameters = Array.Empty<VRCExpressionParameters.Parameter>();
                AssetDatabase.AddObjectToAsset(vrcParams, context.AssetContainer);
                descriptor.expressionParameters = vrcParams;
            }
            
            vrcParams.parameters = vrcParams.parameters.Concat(flareParameters).ToArray();
        }
    }
}