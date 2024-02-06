using System;
using System.Collections.Generic;
using System.Linq;
using Flare.Models;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace Flare.Editor.Passes
{
    internal class MenuizationPass : Pass<MenuizationPass>
    {
        private static readonly string _subId = Guid.NewGuid().ToString();
        
        public override string DisplayName => "Control and Tag Expression Menuization";
        
        protected override void Execute(BuildContext context)
        {
            var flare = context.GetState<FlareAvatarContext>();
            
            var descriptor = context.AvatarDescriptor;
            var expressions = descriptor.expressionsMenu;

            // If for a reason we have no controls, don't generate any menus.
            if (flare.ControlContexts.Count == 0)
                return;
            
            if (expressions == null)
            {
                expressions = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                expressions.controls = new List<VRCExpressionsMenu.Control>(8);
                AssetDatabase.AddObjectToAsset(expressions, context.AssetContainer);
                descriptor.expressionsMenu = expressions;
            }

            foreach (var ctx in flare.ControlContexts)
            {
                if (ctx.Control.Type is not ControlType.Menu)
                    continue;
                
                var info = ctx.Control.MenuItem;
                VRCExpressionsMenu.Control.Parameter param = new() { name = ctx.Id };
                VRCExpressionsMenu.Control menuControl = new()
                {
                    icon = info.Icon,
                    name = info.Name,
                    type = info.Type switch
                    {
                        MenuItemType.Button => VRCExpressionsMenu.Control.ControlType.Button,
                        MenuItemType.Toggle => VRCExpressionsMenu.Control.ControlType.Toggle,
                        MenuItemType.Radial => VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    parameter = info.Type is not MenuItemType.Radial ? param: null,
                    subParameters = info.Type is MenuItemType.Radial ? new [] { param } : Array.Empty<VRCExpressionsMenu.Control.Parameter>()
                };

                // GetComponetsInParent will always be ordered from child -> parent.
                var menus = ctx.Control.GetComponentsInParent<FlareMenu>();
                
                // We reverse it and make a linked list to recursively create submenus as needed.
                LinkedList<FlareMenu> menuList = new(menus.Reverse());

                var targetMenu = expressions;
                var current = menuList.First;
                while (current is not null)
                {
                    var name = current.Value.Name;

                    // I have no idea why, but this broke when using LINQ
                    VRCExpressionsMenu? subMenu = null;
                    foreach (var control in targetMenu.controls)
                    {
                        if (control.type is not VRCExpressionsMenu.Control.ControlType.SubMenu)
                            continue;

                        if (control.subMenu == null)
                            continue;

                        if (control.name != name)
                            continue;

                        subMenu = control.subMenu;
                    }

                    // If it doesn't exist, create it and add it to the parent controls.
                    if (subMenu is null)
                    {
                        subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                        AssetDatabase.AddObjectToAsset(subMenu, context.AssetContainer);
                        targetMenu.controls.Add(new VRCExpressionsMenu.Control
                        {
                            name = name,
                            subMenu = subMenu,
                            icon = current.Value.Icon,
                            type = VRCExpressionsMenu.Control.ControlType.SubMenu
                        });
                    }

                    targetMenu = subMenu;
                    current = current.Next;
                }
                
                targetMenu.controls.Add(menuControl);
            }
            
            // We might've added too many controls, so we auto-generate folders.
            ShrinkAndNestFolderization(expressions, context.AssetContainer);
            RenameFlareSubfolders(expressions);
        }

        private static void ShrinkAndNestFolderization(VRCExpressionsMenu menu, Object container)
        {
            foreach (var control in menu.controls)
                if (control.type is VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null)
                    ShrinkAndNestFolderization(control.subMenu, container);
            
            if (VRCExpressionsMenu.MAX_CONTROLS >= menu.controls.Count)
                return;

            while (menu.controls.Count > VRCExpressionsMenu.MAX_CONTROLS)
            {
                var more = menu.controls.FirstOrDefault(
                    c => c.type is VRCExpressionsMenu.Control.ControlType.SubMenu && c.name == _subId
                );

                if (more == null)
                {
                    var subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                    subMenu.controls.AddRange(menu.controls.Skip(7)); // Take all controls except first seven.
                    subMenu.name = "Flare Subfolder";
                    
                    AssetDatabase.AddObjectToAsset(subMenu, container);
                    
                    more = new VRCExpressionsMenu.Control
                    {
                        name = _subId,
                        subMenu = subMenu,
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu
                    };
                }
                
                // Set controls to first seven + our subfolder
                menu.controls = menu.controls.Take(7).Append(more).ToList();
                
                // Do the same for all submenus
                foreach (var control in menu.controls)
                    if (control.type is VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null)
                        ShrinkAndNestFolderization(control.subMenu, container);
            }
        }

        private static void RenameFlareSubfolders(VRCExpressionsMenu menu)
        {
            // Renames all auto-generated subfolders (which we use a guid to identify them during setup) to "MORE..."
            foreach (var control in menu.controls)
            {
                if (control.type is not VRCExpressionsMenu.Control.ControlType.SubMenu || control.subMenu == null)
                    continue;
                
                if (control.name == _subId)
                    control.name = "MORE...";
                    
                RenameFlareSubfolders(control.subMenu);
            }
        }
    }
}