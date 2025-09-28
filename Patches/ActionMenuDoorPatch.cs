using System;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using DoorBreach;

namespace DoorBreach.Patches
{
    internal class ActionMenuDoorPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod() => typeof(GetActionsClass).GetMethod(nameof(GetActionsClass.smethod_14));


        [PatchPostfix]
        public static void Postfix(ref ActionsReturnClass __result, GamePlayerOwner owner, Door door)
        {
            if (__result == null || __result.Actions == null) return;

            // Add new action to exisitng actions
            ActionsTypesClass breachC4 = new ActionsTypesClass
            {
                Name = "Plant Explosive",
                Action = new Action(() =>
                {
                    DoorBreach.ExplosiveBreachComponent.StartExplosiveBreach(door, owner.Player);
                }),
                Disabled = (!door.IsBreachAngle(owner.Player.Position) || !DoorBreach.ExplosiveBreachComponent.IsValidDoorState(door) ||
                            !DoorBreach.ExplosiveBreachComponent.HasC4Explosives(owner.Player))
            };

            __result.Actions.Add(breachC4);
        }
    }
}