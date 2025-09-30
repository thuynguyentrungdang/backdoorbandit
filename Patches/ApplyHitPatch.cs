using System;
using System.Reflection;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using UnityEngine;
using Fika.Core.Networking;
using Fika.Core.Coop.Utils;
using Fika.Core.Coop.Players;
using LiteNetLib;
using JetBrains.Annotations;

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0007 // Use implicit type
#pragma warning disable CS0169 // The field is never used

namespace DoorBreach
{

    internal class ApplyHit : ModulePatch
    {
        private static BallisticCollider collider;
        private static bool isDoor;
        private static bool isCarTrunk;
        private static bool isLootableContainer;
        private static bool hasHitPoints;
        private static bool validDamage;
        private static Hitpoints hitpoints;
        private static Door door;
        private static Trunk carTrunk;
        private static LootableContainer lootContainer;
        protected override MethodBase GetTargetMethod() => typeof(BallisticCollider).GetMethod(nameof(BallisticCollider.ApplyHit));


        [PatchPostfix]
        public static void PatchPostFix(DamageInfoStruct damageInfo)
        {
            //try catch for random things applying damage that we don't want
            try
            {
                if (ShouldApplyDamage(damageInfo))
                {
                    HandleDamageForEntity(damageInfo);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"BackdoorBandit: Exception in ApplyHitPatch.PatchPostfix(): {ex}");
            }
        }

        private static bool ShouldApplyDamage(DamageInfoStruct damageInfo)
        {

            return damageInfo.Player != null
                && damageInfo.Player.iPlayer.IsYourPlayer
                && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Lamp
                && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Window
                && damageInfo.DamageType != EDamageType.Explosion;
        }

        private static void HandleDamageForEntity(DamageInfoStruct damageInfo)
        {
            BallisticCollider collider = damageInfo.HittedBallisticCollider;
            if (collider == null) return; // Unity null-safe check

            bool validDamage = DoorBreachPlugin.PlebMode.Value;

            bool isCarTrunk = collider.GetComponentInParent<Trunk>() != null;
            bool isLootableContainer = collider.GetComponentInParent<LootableContainer>() != null;
            bool isDoor = collider.GetComponentInParent<Door>() != null;
            bool hasHitPoints = collider.GetComponentInParent<Hitpoints>() != null;

            Logger.LogDebug($"BackdoorBandit: isCarTrunk: {isCarTrunk}, isLootableContainer: {isLootableContainer}, isDoor: {isDoor}, hasHitPoints: {hasHitPoints}");
            Logger.LogDebug($"BackdoorBandit: validDamage initialized to: {validDamage}");
            Logger.LogDebug($"BackdoorBandit: hasHitPoints: {hasHitPoints}");
            if (!hasHitPoints) return;

            if (isCarTrunk) HandleCarTrunkDamage(damageInfo, collider, ref validDamage);
            if (isLootableContainer) HandleLootableContainerDamage(damageInfo, collider, ref validDamage);
            if (isDoor) HandleDoorDamage(damageInfo, collider, ref validDamage);
        }

        #region DamageApplication
        private static void HandleCarTrunkDamage(DamageInfoStruct damageInfo, BallisticCollider collider, ref bool validDamage)
        {
            if (!DoorBreachPlugin.PlebMode.Value && DoorBreachPlugin.OpenCarDoors.Value)
            {
                DamageUtility.CheckCarWeaponAndAmmo(damageInfo, ref validDamage);
            }

            HandleDamage(damageInfo, collider, ref validDamage, "Car Trunk", (hitpoints, entity) =>
            {
                if (hitpoints.hitpoints <= 0)
                {
                    Trunk carTrunk = entity.GetComponentInParent<Trunk>();
                    OpenDoorIfNotAlreadyOpen(carTrunk, damageInfo.Player.AIData.Player, EInteractionType.Open);
                }
            });
        }

        private static void HandleLootableContainerDamage(DamageInfoStruct damageInfo, BallisticCollider collider, ref bool validDamage)
        {
            if (!DoorBreachPlugin.PlebMode.Value && DoorBreachPlugin.OpenLootableContainers.Value)
            {
                DamageUtility.CheckLootableContainerWeaponAndAmmo(damageInfo, ref validDamage);
            }

            HandleDamage(damageInfo, collider, ref validDamage, "Lootable Container", (hitpoints, entity) =>
            {
                if (hitpoints.hitpoints <= 0)
                {
                    LootableContainer lootContainer = entity.GetComponentInParent<LootableContainer>();
                    OpenDoorIfNotAlreadyOpen(lootContainer, damageInfo.Player.AIData.Player, EInteractionType.Open);
                }
            });
        }

        internal static void HandleDoorDamage(DamageInfoStruct damageInfo, BallisticCollider collider, ref bool validDamage)
        {
            if (!DoorBreachPlugin.PlebMode.Value)
            {
                Logger.LogDebug("BackdoorBandit: Checking Door Weapon and Ammo");
                DamageUtility.CheckDoorWeaponAndAmmo(damageInfo, ref validDamage);
            }

            Logger.LogDebug($"BackdoorBandit: validDamage before HandleDamage: {validDamage}, calling HandleDamage()");
            HandleDamage(damageInfo, collider, ref validDamage, "Door", (hitpoints, entity) =>
            {
                WorldInteractiveObject door = entity.GetComponentInParent<WorldInteractiveObject>();

                DoorBreachComponent.Logger.LogDebug("[Door info]");
                DoorBreachComponent.Logger.LogDebug($"KeyId: {door.KeyId}");
                DoorBreachComponent.Logger.LogDebug($"DoorState: {door.DoorState}");
                DoorBreachComponent.Logger.LogDebug($"InitialDoorState: {door.InitialDoorState}");

                if (hitpoints.hitpoints <= 0)
                {
                    OpenDoorIfNotAlreadyOpen(door, damageInfo.Player.AIData.Player, EInteractionType.Breach);
                }
            });
        }

        internal static void HandleDamage(DamageInfoStruct damageInfo, BallisticCollider collider, ref bool validDamage, string entityName, Action<Hitpoints, GameObject> onHitpointsZero)
        {
            Hitpoints hitpoints = collider.GetComponentInParent<Hitpoints>();

            if (!validDamage)
                return;

            Logger.LogInfo($"BackdoorBandit: Applying Hit Damage {damageInfo.Damage} hitpoints to {entityName}");
            hitpoints.hitpoints -= damageInfo.Damage;

            onHitpointsZero?.Invoke(hitpoints, collider.gameObject);

        }
        internal static void OpenDoorIfNotAlreadyOpen<T>(T entity, Player player, EInteractionType interactionType) where T : class
        {
            CoopPlayer coopPlayer = player as CoopPlayer;
            if (entity is Door door)
            {
                if (door.DoorState != EDoorState.Open)
                {
                    door.DoorState = EDoorState.Shut;
                    bool doorUsesAnim = door.interactWithoutAnimation;

                    door.interactWithoutAnimation = true;
                    player.CurrentManagedState.ExecuteDoorInteraction(door, new InteractionResult(interactionType), null, player);
                    door.interactWithoutAnimation = doorUsesAnim;

                    // Create packet with info that all players will need
                    SyncOpenStatePacket packet = new SyncOpenStatePacket()
                    {
                        netID = coopPlayer.NetId,
                        objectID = door.Id,
                        objectType = 0
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
                }
            }

            if (entity is LootableContainer container)
            {
                if (container.DoorState != EDoorState.Open)
                {
                    container.DoorState = EDoorState.Shut;
                    // Get the original value of whether the container uses an animation or not
                    bool containerUsesAnim = container.interactWithoutAnimation;

                    // Set the container to not use an animation when opening
                    container.interactWithoutAnimation = true;

                    // Unlock the container
                    container.Open();

                    // Open the container
                    player.CurrentManagedState.ExecuteDoorInteraction(container, new InteractionResult(interactionType), null, player);

                    // Set the container's animation requirement back to the default.
                    container.interactWithoutAnimation = containerUsesAnim;

                    SyncOpenStatePacket packet = new SyncOpenStatePacket()
                    {
                        netID = coopPlayer.NetId,
                        objectID = container.Id,
                        objectType = 1
                    };

                    if (FikaBackendUtils.IsServer)
                    {
                        Singleton<FikaServer>.Instance.SendDataToAll(ref packet,
                            DeliveryMethod.ReliableOrdered);
                    }
                    else if (FikaBackendUtils.IsClient)
                    {
                        Singleton<FikaClient>.Instance.SendData(ref packet,
                            DeliveryMethod.ReliableOrdered);
                    }
                }
            }
            if (entity is Trunk trunk)
            {

                if (trunk.DoorState != EDoorState.Open)
                {
                    trunk.DoorState = EDoorState.Shut;

                    // Get the original value of whether the container uses an animation or not
                    bool trunkUsesAnim = trunk.interactWithoutAnimation;

                    // Set the container to not use an animation when opening
                    trunk.interactWithoutAnimation = true;

                    trunk.Open();
                    player.CurrentManagedState.ExecuteDoorInteraction(trunk, new InteractionResult(interactionType), null, player);

                    trunk.interactWithoutAnimation = trunkUsesAnim;

                    SyncOpenStatePacket packet = new SyncOpenStatePacket()
                    {
                        netID = coopPlayer.NetId,
                        objectID = trunk.Id,
                        objectType = 2
                    };

                    if (FikaBackendUtils.IsServer)
                    {
                        Singleton<FikaServer>.Instance.SendDataToAll(ref packet,
                            DeliveryMethod.ReliableOrdered);
                    }
                    else if (FikaBackendUtils.IsClient)
                    {
                        Singleton<FikaClient>.Instance.SendData(ref packet,
                            DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        internal static void CustomExecuteDoorInteraction(WorldInteractiveObject interactive, InteractionResult interactionResult, [CanBeNull] Action callback, Player user)
        {
            interactive.interactWithoutAnimation = true;
            interactive.SetUser(user);
            interactive.LockForInteraction();
            interactive.Interact(interactionResult);
        }
#endregion
    }

}
