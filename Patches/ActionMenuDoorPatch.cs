using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using DoorBreach.Fika;

namespace DoorBreach.Patches
{
    internal class ActionMenuDoorPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod() => typeof(GetActionsClass).GetMethod(nameof(GetActionsClass.smethod_14));


        [PatchPostfix]
        public static void Postfix(ref ActionsReturnClass __result, GamePlayerOwner owner, Door door)
        {
            if (__result == null || 
                __result.Actions == null) 
                return;

            // Add new action to exisitng actions
            ActionsTypesClass breachC4 = new ActionsTypesClass
            {
                Name = "Plant Explosive",
                Action = () =>
                {
                    ExplosiveBreachComponent.StartExplosiveBreach(door, owner.Player);
                    ExplosiveBreachComponent.RemoveItemFromPlayerInventory(owner.Player);

                    FikaBridge.SendPlantC4PacketPacket(owner.Player, door.Id, DoorBreachPlugin.explosiveTimerInSec);
                },
                
                Disabled = !door.IsBreachAngle(owner.Player.Position) || 
                           !ExplosiveBreachComponent.IsValidDoorState(door) ||
                           !ExplosiveBreachComponent.HasC4Explosives(owner.Player)
            };

            __result.Actions.Add(breachC4);
        }
    }
}