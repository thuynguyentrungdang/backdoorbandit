using System;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Comfort.Common;
using LiteNetLib;
using Fika.Core.Coop.Players;

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
                    ExplosiveBreachComponent.StartExplosiveBreach(door, owner.Player);

                    CoopPlayer player = owner.Player as CoopPlayer;

                    PlantC4Packet packet = new PlantC4Packet
                    {
                        netID = player.NetId,
                        doorID = door.Id,
                        C4Timer = DoorBreachPlugin.explosiveTimerInSec.Value,
                    };

                    if (FikaBackendUtils.IsServer)
                    {
                        // Forward the packet to all clients
                        Singleton<FikaServer>.Instance.SendDataToAll(ref packet,
                            DeliveryMethod.ReliableOrdered);
                        // ReliableOrdered = ensures the packet is received, re-sends it if it fails
                    }
                    else if (FikaBackendUtils.IsClient)
                    {
                        // If we're a client, send it to the host so they can forward it (Check Plugin.cs for behavior)
                        Singleton<FikaClient>.Instance.SendData(ref packet,
                            DeliveryMethod.ReliableOrdered);
                    }
                }),
                Disabled = !door.IsBreachAngle(owner.Player.Position) || !ExplosiveBreachComponent.IsValidDoorState(door) ||
                            !ExplosiveBreachComponent.HasC4Explosives(owner.Player)
            };

            __result.Actions.Add(breachC4);
        }
    }
}