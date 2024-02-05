/*
 * MIT License
 *
 * Copyright (c) 2022 bd_
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using nadena.dev.ndmf.util;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Flare.Editor.Animation
{
    internal class AnimatorCombiner
    {
        private int _controllerBaseLayer;
        private Dictionary<Object, Object>? _cloneMap;
        
        private readonly DeepClone _deepClone;
        private readonly AnimatorController _combined;

        private List<AnimatorControllerLayer> _layers = new();
        private readonly Dictionary<string, AnimatorControllerParameter> _parameters = new();
        private readonly Dictionary<KeyValuePair<string, AnimatorStateMachine>, AnimatorStateMachine> _stateMachines = new();

        public VRC_AnimatorLayerControl.BlendableLayer? BlendableLayer;

        public AnimatorCombiner(BuildContext context, string assetName)
        {
            _combined = new AnimatorController();
            if (context.AssetContainer != null && EditorUtility.IsPersistent(context.AssetContainer))
            {
                AssetDatabase.AddObjectToAsset(_combined, context.AssetContainer);
            }

            _combined.name = assetName;

            _deepClone = new DeepClone(context);
        }

        public AnimatorController Finish()
        {
            PruneEmptyLayers();

            _combined.parameters = _parameters.Values.ToArray();
            _combined.layers = _layers.ToArray();
            return _combined;
        }

        private void PruneEmptyLayers()
        {
            var originalLayers = _layers;
            int[] layerIndexMappings = new int[originalLayers.Count];

            List<AnimatorControllerLayer> newLayers = new();

            for (int i = 0; i < originalLayers.Count; i++)
            {
                if (i > 0 && IsEmptyLayer(originalLayers[i]))
                {
                    layerIndexMappings[i] = -1;
                }
                else
                {
                    layerIndexMappings[i] = newLayers.Count;
                    newLayers.Add(originalLayers[i]);
                }
            }

            foreach (var layer in newLayers)
            {
                if (layer.stateMachine == null) continue;

                foreach (var asset in layer.stateMachine.ReferencedAssets(includeScene: false))
                {
                    if (asset is AnimatorState alc)
                    {
                        alc.behaviours = AdjustStateBehaviors(alc.behaviours);
                    }
                    else if (asset is AnimatorStateMachine asm)
                    {
                        asm.behaviours = AdjustStateBehaviors(asm.behaviours);
                    }
                }
            }

            _layers = newLayers;

            StateMachineBehaviour[] AdjustStateBehaviors(StateMachineBehaviour[] behaviours)
            {
                if (behaviours.Length == 0) return behaviours;

                var newBehaviors = new List<StateMachineBehaviour>();
                foreach (var b in behaviours)
                {
                    if (b is VRCAnimatorLayerControl alc && alc.playable == BlendableLayer)
                    {
                        int newLayer = -1;
                        if (alc.layer >= 0 && alc.layer < layerIndexMappings.Length)
                        {
                            newLayer = layerIndexMappings[alc.layer];
                        }

                        if (newLayer != -1)
                        {
                            alc.layer = newLayer;
                            newBehaviors.Add(alc);
                        }
                    }
                    else
                    {
                        newBehaviors.Add(b);
                    }
                }

                return newBehaviors.ToArray();
            }
        }

        private bool IsEmptyLayer(AnimatorControllerLayer layer)
        {
            if (layer.syncedLayerIndex >= 0) return false;
            if (layer.avatarMask != null) return false;

            return layer.stateMachine == null
                   || (layer.stateMachine.states.Length == 0 && layer.stateMachine.stateMachines.Length == 0);
        }

        public void AddController(string basePath, AnimatorController controller, bool? writeDefaults,
            bool forceFirstLayerWeight = false)
        {
            _controllerBaseLayer = _layers.Count;
            _cloneMap = new Dictionary<Object, Object>();

            foreach (var param in controller.parameters)
            {
                if (_parameters.TryGetValue(param.name, out var acp))
                {
                    if (acp.type != param.type)
                    {
                        Debug.LogError("Animator merge parameter type mismatch");
                    }
                    continue;
                }

                _parameters.Add(param.name, param);
            }

            bool first = true;
            var layers = controller.layers;
            foreach (var layer in layers)
            {
                InsertLayer(basePath, layer, first, writeDefaults, layers);
                if (first && forceFirstLayerWeight)
                {
                    _layers[^1].defaultWeight = 1;
                }

                first = false;
            }
        }

        public void AddOverrideController(string basePath, AnimatorOverrideController overrideController,
            bool? writeDefaults)
        {
            var controller = overrideController.runtimeAnimatorController as AnimatorController;
            if (controller == null)
                return;
            
            _deepClone.OverrideController = overrideController;
            try
            {
                AddController(basePath, controller, writeDefaults);
            }
            // ReSharper disable once RedundantEmptyFinallyBlock
            finally
            {
            }
        }

        private void InsertLayer(string basePath, AnimatorControllerLayer layer, bool first,
            bool? writeDefaults, IReadOnlyList<AnimatorControllerLayer> layers)
        {
            if (_cloneMap == null)
                return;
            
            var newLayer = new AnimatorControllerLayer
            {
                name = layer.name,
                avatarMask = layer.avatarMask,
                blendingMode = layer.blendingMode,
                defaultWeight = first ? 1 : layer.defaultWeight,
                syncedLayerIndex = layer.syncedLayerIndex,
                syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming,
                iKPass = layer.iKPass,
                stateMachine = MapStateMachine(basePath, layer.stateMachine),
            };

            UpdateWriteDefaults(newLayer.stateMachine, writeDefaults);

            
            if (newLayer.syncedLayerIndex != -1 && newLayer.syncedLayerIndex >= 0 &&
                newLayer.syncedLayerIndex < layers.Count)
            {
                // Transfer any motion overrides onto the new synced layer
                var baseLayer = layers[newLayer.syncedLayerIndex];
                foreach (var state in WalkAllStates(baseLayer.stateMachine))
                {
                    var overrideMotion = layer.GetOverrideMotion(state);
                    if (overrideMotion != null)
                    {
                        newLayer.SetOverrideMotion((AnimatorState)_cloneMap[state], overrideMotion);
                    }

                    var overrideBehaviors = (StateMachineBehaviour[])layer.GetOverrideBehaviours(state)?.Clone()!;
                    for (int i = 0; i < overrideBehaviors.Length; i++)
                    {
                        overrideBehaviors[i] = _deepClone.DoClone(overrideBehaviors[i])!;
                        AdjustBehavior(overrideBehaviors[i]);
                    }

                    newLayer.SetOverrideBehaviours((AnimatorState)_cloneMap[state], overrideBehaviors);
                }

                newLayer.syncedLayerIndex += _controllerBaseLayer;
            }

            _layers.Add(newLayer);
        }

        private static IEnumerable<AnimatorState> WalkAllStates(AnimatorStateMachine animatorStateMachine)
        {
            HashSet<Object> visited = new();

            foreach (var state in VisitStateMachine(animatorStateMachine))
            {
                yield return state;
            }

            yield break;

            IEnumerable<AnimatorState> VisitStateMachine(AnimatorStateMachine layerStateMachine)
            {
                if (!visited.Add(layerStateMachine)) yield break;

                foreach (var state in layerStateMachine.states)
                {
                    if (state.state == null) continue;

                    yield return state.state;
                }

                foreach (var child in layerStateMachine.stateMachines)
                {
                    if (child.stateMachine == null) continue;

                    if (!visited.Add(child.stateMachine))
                        continue;
                    
                    foreach (var state in VisitStateMachine(child.stateMachine))
                    {
                        yield return state;
                    }
                }
            }
        }

        private static void UpdateWriteDefaults(AnimatorStateMachine stateMachine, bool? writeDefaults)
        {
            if (!writeDefaults.HasValue)
                return;

            var queue = new Queue<AnimatorStateMachine>();
            queue.Enqueue(stateMachine);
            while (queue.Count > 0)
            {
                var sm = queue.Dequeue();
                foreach (var state in sm.states)
                {
                    state.state.writeDefaultValues = writeDefaults.Value;
                }

                foreach (var child in sm.stateMachines)
                {
                    queue.Enqueue(child.stateMachine);
                }
            }
        }

        private AnimatorStateMachine MapStateMachine(string basePath, AnimatorStateMachine layerStateMachine)
        {
            var cacheKey = new KeyValuePair<string, AnimatorStateMachine>(basePath, layerStateMachine);

            if (_stateMachines.TryGetValue(cacheKey, out var asm))
            {
                return asm;
            }

            asm = _deepClone.DoClone(layerStateMachine, basePath, _cloneMap);

            foreach (var state in WalkAllStates(asm!))
            {
                foreach (var behavior in state.behaviours)
                {
                    AdjustBehavior(behavior);
                }
            }

            _stateMachines[cacheKey] = asm!;
            return asm!;
        }

        private void AdjustBehavior(StateMachineBehaviour behavior)
        {
            switch (behavior)
            {
                case VRCAnimatorLayerControl layerControl:
                {
                    // intra-animator cases.
                    layerControl.layer += _controllerBaseLayer;
                    break;
                }
            }
        }
    }
}