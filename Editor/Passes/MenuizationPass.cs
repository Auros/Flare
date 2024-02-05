using System;
using System.Collections.Generic;
using System.Linq;
using Flare.Models;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Flare.Editor.Passes
{
    internal class MenuizationPass : Pass<MenuizationPass>
    {
        public override string DisplayName => "Control and Tag Expression Menuization";
        
        protected override void Execute(BuildContext context)
        {
            var flare = context.GetState<FlareAvatarContext>();
            
            var descriptor = context.AvatarDescriptor;
            var expressions = descriptor.expressionsMenu;

            
            /*var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.name = "[Flare] Expression Menu";
            AssetDatabase.AddObjectToAsset(menu, context.AssetContainer);*/
            
            /*expressions.controls.Add(new VRCExpressionsMenu.Control
            {
                subMenu = menu,
                name = "Flare (Test)",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu
            });*/
            
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
        }
    }
}