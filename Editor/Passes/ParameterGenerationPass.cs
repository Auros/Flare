using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Flare.Editor.Passes
{
    internal class ParameterGenerationPass : Pass<ParameterGenerationPass>
    {
        public override string DisplayName => "Expression Parameter Generator";

        protected override void Execute(BuildContext context)
        {
            // Generate temporary parameter object.
            var descriptor = context.AvatarDescriptor;
            var vrcParams = descriptor.expressionParameters;

            // If persistent, copy to avoid messing with the original.
            if (!EditorUtility.IsPersistent(vrcParams))
                return;
            
            var newParams = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            newParams.name = "[Flare] Expression Parameters";
            newParams.parameters = vrcParams.parameters.Select(p => new VRCExpressionParameters.Parameter
            {
                name = p.name,
                saved = p.saved,
                valueType = p.valueType,
                defaultValue = p.defaultValue,
                networkSynced = p.networkSynced
            }).ToArray();
            
            AssetDatabase.AddObjectToAsset(newParams, context.AssetContainer);
            descriptor.expressionParameters = vrcParams;
        }
    }
}