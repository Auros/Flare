// MIT License
// 
// Copyright (c) 2022 bd_
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

// https://github.com/bdunderscore/modular-avatar/blob/6dcea7fa5eaacc66a3500ab36ddcdbbb52abd836/Editor/Animation/AnimationUtil.cs
namespace Flare.Editor.Animation
{
    internal static class AnimationUtilities
    {
        private const string _samplePathPackage = "Packages/com.vrchat.avatars/Samples/AV3 Demo Assets/Animation/Controllers";

        private const string _samplePathLegacy = "Assets/VRCSDK/Examples3/Animation/Controllers";

        private const string _guidGestureHandsonlyMask = "b2b8bad9583e56a46a3e21795e96ad92";

        public static AnimatorController? DeepCloneAnimator(BuildContext context, RuntimeAnimatorController controller)
        {
            if (controller == null)
                return null;

            var merger = new AnimatorCombiner(context, controller.name + " (cloned)");
            switch (controller)
            {
                case AnimatorController ac:
                    merger.AddController("", ac, null);
                    break;
                case AnimatorOverrideController oac:
                    merger.AddOverrideController("", oac, null);
                    break;
                default:
                    throw new Exception("Unknown RuntimeAnimatorContoller type " + controller.GetType());
            }

            return merger.Finish();
        }

        internal static void CloneAllControllers(BuildContext context)
        {
            if (context.GetState<FlareAvatarContext>().IsEmpty)
                return;
            
            // Ensure all of the controllers on the avatar descriptor point to temporary assets.
            // This helps reduce the risk that we'll accidentally modify the original assets.
            context.AvatarDescriptor.baseAnimationLayers =
                CloneLayers(context, context.AvatarDescriptor.baseAnimationLayers);
            context.AvatarDescriptor.specialAnimationLayers =
                CloneLayers(context, context.AvatarDescriptor.specialAnimationLayers);
        }

        private static VRCAvatarDescriptor.CustomAnimLayer[]? CloneLayers(BuildContext context, VRCAvatarDescriptor.CustomAnimLayer[]? layers)
        {
            if (layers == null)
                return null;

            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                
                // FLARE SPECIFIC: We only work with the FX layer (for now)
                if (layer.type is not VRCAvatarDescriptor.AnimLayerType.FX)
                    continue;
                
                if (layer.animatorController != null && !context.IsTemporaryAsset(layer.animatorController))
                {
                    layer.animatorController = DeepCloneAnimator(context, layer.animatorController);
                }
                layers[i] = layer;
            }

            return layers;
        }

        public static AnimatorController? GetOrInitializeController(this BuildContext context, VRCAvatarDescriptor.AnimLayerType type)
        {
            return FindLayer(context.AvatarDescriptor.baseAnimationLayers).AsNullable() ?? FindLayer(context.AvatarDescriptor.specialAnimationLayers);

            AnimatorController? FindLayer(IList<VRCAvatarDescriptor.CustomAnimLayer> layers)
            {
                for (int i = 0; i < layers.Count; i++)
                {
                    var layer = layers[i];
                    if (layer.type != type)
                        continue;

                    if (layer.animatorController != null && !layer.isDefault)
                        return (layer.animatorController as AnimatorController)!;
                    
                    layer.animatorController = ResolveLayerController(layer);
                    if (type == VRCAvatarDescriptor.AnimLayerType.Gesture)
                    {
                        layer.mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(
                            AssetDatabase.GUIDToAssetPath(_guidGestureHandsonlyMask)
                        );
                    }

                    layers[i] = layer;

                    return (layer.animatorController as AnimatorController)!;
                }

                return null;
            }
        }


        private static AnimatorController? ResolveLayerController(VRCAvatarDescriptor.CustomAnimLayer layer)
        {
            AnimatorController? controller = null;
            if (!layer.isDefault && layer.animatorController != null && layer.animatorController is AnimatorController c)
            {
                controller = c;
            }
            else
            {
                string? name = layer.type switch
                {
                    VRCAvatarDescriptor.AnimLayerType.Action => "Action",
                    VRCAvatarDescriptor.AnimLayerType.Additive => "Idle",
                    VRCAvatarDescriptor.AnimLayerType.Base => "Locomotion",
                    VRCAvatarDescriptor.AnimLayerType.Gesture => "Hands",
                    VRCAvatarDescriptor.AnimLayerType.Sitting => "Sitting",
                    VRCAvatarDescriptor.AnimLayerType.FX => "Face",
                    VRCAvatarDescriptor.AnimLayerType.TPose => "UtilityTPose",
                    VRCAvatarDescriptor.AnimLayerType.IKPose => "UtilityIKPose",
                    _ => null
                };

                if (name == null)
                    return controller;
                
                name = "/vrc_AvatarV3" + name + "Layer.controller";

                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(_samplePathPackage + name);
                if (controller == null)
                    controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(_samplePathLegacy + name);
            }

            return controller;
        }

        public static bool IsProxyAnimation(this Motion m)
        {
            var path = AssetDatabase.GetAssetPath(m);

            // This is a fairly wide condition in order to deal with:
            // 1. Future additions of proxy animations (so GUIDs are out)
            // 2. Unitypackage based installations of the VRCSDK
            // 3. VCC based installations of the VRCSDK
            // 4. Very old VCC based installations of the VRCSDK where proxy animations were copied into Assets
            return path.Contains("/AV3 Demo Assets/Animation/ProxyAnim/proxy")
                   || path.Contains("/VRCSDK/Examples3/Animation/ProxyAnim/proxy")
                   || path.StartsWith("Packages/com.vrchat.");
        }

        /// <summary>
        /// Enumerates all states in an animator controller
        /// </summary>
        /// <param name="ac"></param>
        /// <returns></returns>
        internal static IEnumerable<AnimatorState> States(AnimatorController ac)
        {
            HashSet<AnimatorStateMachine> visitedStateMachines = new();
            Queue<AnimatorStateMachine> pending = new();

            foreach (var layer in ac.layers)
                if (layer.stateMachine != null)
                    pending.Enqueue(layer.stateMachine);

            while (pending.Count > 0)
            {
                var next = pending.Dequeue();
                if (!visitedStateMachines.Add(next))
                    continue;

                foreach (var child in next.stateMachines)
                    if (child.stateMachine != null)
                        pending.Enqueue(child.stateMachine);

                foreach (var state in next.states)
                    yield return state.state;
            }
        }
    }
}