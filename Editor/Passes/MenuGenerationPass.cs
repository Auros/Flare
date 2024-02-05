using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Flare.Editor.Passes
{
    public class MenuGenerationPass : Pass<MenuGenerationPass>
    {
        public override string DisplayName => "Expression Menu Generator";

        protected override void Execute(BuildContext context)
        {
            // Because of the way Flare Folders work, we clone every submenu.
            var descriptor = context.AvatarDescriptor;

            foreach (var control in descriptor.expressionsMenu.controls)
                CloneSubmenu(control, context.AssetContainer);
            
            if (!EditorUtility.IsPersistent(descriptor.expressionsMenu))
                return;
            
            var newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            newMenu.controls = descriptor.expressionsMenu.controls.ToList();
            newMenu.name = "[Flare] Expression Menu";
            
            AssetDatabase.AddObjectToAsset(newMenu, context.AssetContainer);
            descriptor.expressionsMenu = newMenu;
        }

        private static void CloneSubmenu(VRCExpressionsMenu.Control control, Object container)
        {
            if (control.subMenu == null)
                return;

            if (control.type is not VRCExpressionsMenu.Control.ControlType.SubMenu)
                return;

            if (!EditorUtility.IsPersistent(control.subMenu))
                return;

            var newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            newMenu.name = $"[Flare] {control.subMenu.name} (Clone)";
            newMenu.controls = control.subMenu.controls.ToList();
            control.subMenu = newMenu;
            
            AssetDatabase.AddObjectToAsset(newMenu, container);
            
            foreach (var subMenuControl in control.subMenu.controls)
                CloneSubmenu(subMenuControl, container);
        }
    }
}