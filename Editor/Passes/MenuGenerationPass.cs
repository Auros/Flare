﻿using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Flare.Editor.Passes
{
    internal class MenuGenerationPass : Pass<MenuGenerationPass>
    {
        public override string DisplayName => "Expression Menu Generator";

        protected override void Execute(BuildContext context)
        {
            // Because of the way Flare Folders work, we clone every submenu.
            var descriptor = context.AvatarDescriptor;
            descriptor.expressionsMenu = Clone(descriptor.expressionsMenu, context.AssetContainer);
        }

        private static VRCExpressionsMenu? Clone(VRCExpressionsMenu? menu, Object container)
        {
            if (menu == null)
                return null;

            // Clone persistent menu
            if (EditorUtility.IsPersistent(menu))
            {
                var newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                newMenu.name = $"[Flare] {menu.name} (Clone)";
                newMenu.controls = menu.controls.Select(old => new VRCExpressionsMenu.Control
                {
                    icon = old.icon,
                    labels = old.labels.Select(l => l).ToArray(), // Copy labels directly and make new array
                    name = old.name,
                    parameter = new VRCExpressionsMenu.Control.Parameter {name = old.parameter.name },
                    style = old.style,
                    subMenu = old.subMenu,
                    subParameters = old.subParameters.Select(oldSub => new VRCExpressionsMenu.Control.Parameter { name = oldSub.name }).ToArray(),
                    type = old.type,
                    value = old.value
                }).ToList();
                AssetDatabase.AddObjectToAsset(newMenu, container);
                menu = newMenu;
            }

            foreach (var control in menu.controls)
                if (control.type is VRCExpressionsMenu.Control.ControlType.SubMenu)
                    control.subMenu = Clone(control.subMenu, container);

            return menu;
        }
    }
}