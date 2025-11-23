using System;
using System.Reflection;
using SPT.Reflection.Patching;
using DoorBreach.Fika;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using UnityEngine;
using JetBrains.Annotations;

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0007 // Use implicit type
#pragma warning disable CS0169 // The field is never used

namespace DoorBreach.Patches;

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

    protected override MethodBase GetTargetMethod() =>
        typeof(BallisticCollider).GetMethod(nameof(BallisticCollider.ApplyHit));


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

        bool validDamage = DoorBreachPlugin.PlebMode;

        bool isCarTrunk = collider.GetComponentInParent<Trunk>() != null;
        bool isLootableContainer = collider.GetComponentInParent<LootableContainer>() != null;
        bool isDoor = collider.GetComponentInParent<Door>() != null;
        bool hasHitPoints = collider.GetComponentInParent<Hitpoints>() != null;

        Logger.LogDebug(
            $"BackdoorBandit: isCarTrunk: {isCarTrunk}, isLootableContainer: {isLootableContainer}, isDoor: {isDoor}, hasHitPoints: {hasHitPoints}");
        Logger.LogDebug($"BackdoorBandit: validDamage initialized to: {validDamage}");
        Logger.LogDebug($"BackdoorBandit: hasHitPoints: {hasHitPoints}");
        if (!hasHitPoints) return;

        if (isCarTrunk) HandleCarTrunkDamage(damageInfo, collider, ref validDamage);
        if (isLootableContainer) HandleLootableContainerDamage(damageInfo, collider, ref validDamage);
        if (isDoor) HandleDoorDamage(damageInfo, collider, ref validDamage);
    }

    #region DamageApplication

    private static void HandleCarTrunkDamage(DamageInfoStruct damageInfo, BallisticCollider collider,
        ref bool validDamage)
    {
        if (!DoorBreachPlugin.PlebMode && 
            DoorBreachPlugin.OpenCarDoors)
            DamageUtility.CheckCarWeaponAndAmmo(damageInfo, ref validDamage);

        HandleDamage(damageInfo, collider, ref validDamage, "Car Trunk", (hitpoints, entity) =>
        {
            if (hitpoints.hitpoints <= 0)
            {
                Trunk carTrunk = entity.GetComponentInParent<Trunk>();
                OpenDoorIfNotAlreadyOpen(carTrunk, damageInfo.Player.AIData.Player, EInteractionType.Open);
            }
        });
    }

    private static void HandleLootableContainerDamage(DamageInfoStruct damageInfo, 
                                                    BallisticCollider collider, 
                                                    ref bool validDamage)
    {
        if (!DoorBreachPlugin.PlebMode && 
            DoorBreachPlugin.OpenLootableContainers)
            DamageUtility.CheckLootableContainerWeaponAndAmmo(damageInfo, ref validDamage);

        HandleDamage(damageInfo, collider, ref validDamage, "Lootable Container", (hitpoints, entity) =>
        {
            if (!(hitpoints.hitpoints <= 0))
                return;

            LootableContainer lootContainer = entity.GetComponentInParent<LootableContainer>();
            OpenDoorIfNotAlreadyOpen(lootContainer, damageInfo.Player.AIData.Player, EInteractionType.Open);
        });
    }

    internal static void HandleDoorDamage(DamageInfoStruct damageInfo, 
                                        BallisticCollider collider, 
                                        ref bool validDamage)
    {
        if (!DoorBreachPlugin.PlebMode)
        {
            DamageUtility.CheckDoorWeaponAndAmmo(damageInfo, ref validDamage);

            if (validDamage)
                DamageUtility.IsMarkedRoom(collider.GetComponentInParent<Door>(), ref validDamage);
        }

        Logger.LogDebug($"BackdoorBandit: validDamage before HandleDamage: {validDamage}, calling HandleDamage()");
        HandleDamage(damageInfo, collider, ref validDamage, "Door", (hitpoints, entity) =>
        {
            WorldInteractiveObject door = entity.GetComponentInParent<WorldInteractiveObject>();

            DoorBreachComponent.Logger.LogDebug("[Door info]");
            DoorBreachComponent.Logger.LogDebug($"DoorId: {door.Id}");
            DoorBreachComponent.Logger.LogDebug($"KeyId: {door.KeyId}");
            DoorBreachComponent.Logger.LogDebug($"DoorState: {door.DoorState}");
            DoorBreachComponent.Logger.LogDebug($"InitialDoorState: {door.InitialDoorState}");

            if (hitpoints.hitpoints <= 0)
            {
                OpenDoorIfNotAlreadyOpen(door, damageInfo.Player.AIData.Player, EInteractionType.Breach);
            }
        });


    }

    internal static void HandleDamage(DamageInfoStruct damageInfo, BallisticCollider collider, ref bool validDamage,
        string entityName, Action<Hitpoints, GameObject> onHitpointsZero)
    {
        Hitpoints hitpoints = collider.GetComponentInParent<Hitpoints>();

        if (!validDamage)
            return;

        Logger.LogInfo($"BackdoorBandit: Applying Hit Damage {damageInfo.Damage} hitpoints to {entityName}");
        hitpoints.hitpoints -= damageInfo.Damage;

        onHitpointsZero?.Invoke(hitpoints, collider.gameObject);

    }

    internal static void OpenDoorIfNotAlreadyOpen<T>(T entity, Player player, EInteractionType interactionType)
        where T : class
    {
        switch (entity)
        {
            case Door door:
            {
                if (door.DoorState != EDoorState.Open)
                {
                    door.DoorState = EDoorState.Shut;
                    bool doorUsesAnim = door.interactWithoutAnimation;

                    door.interactWithoutAnimation = true;
                    player.CurrentManagedState.ExecuteDoorInteraction(door, new InteractionResult(interactionType),
                        null, player);
                    door.interactWithoutAnimation = doorUsesAnim;

                    // Create packet with info that all players will need
                    FikaBridge.SendSyncOpenStatePacket(player, door.Id, 0);
                }

                break;
            }
            case LootableContainer container:
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
                    player.CurrentManagedState.ExecuteDoorInteraction(container,
                        new InteractionResult(interactionType), null, player);

                    // Set the container's animation requirement back to the default.
                    container.interactWithoutAnimation = containerUsesAnim;

                    FikaBridge.SendSyncOpenStatePacket(player, container.Id, 1);
                }

                break;
            }
            case Trunk trunk:
            {
                if (trunk.DoorState != EDoorState.Open)
                {
                    trunk.DoorState = EDoorState.Shut;

                    // Get the original value of whether the container uses an animation or not
                    bool trunkUsesAnim = trunk.interactWithoutAnimation;

                    // Set the container to not use an animation when opening
                    trunk.interactWithoutAnimation = true;

                    trunk.Open();
                    player.CurrentManagedState.ExecuteDoorInteraction(trunk, new InteractionResult(interactionType),
                        null, player);

                    trunk.interactWithoutAnimation = trunkUsesAnim;

                    FikaBridge.SendSyncOpenStatePacket(player, trunk.Id, 2);
                }

                break;
            }
        }
    }

    internal static void CustomExecuteDoorInteraction(WorldInteractiveObject interactive,
        InteractionResult interactionResult, [CanBeNull] Action callback, Player user)
    {
        interactive.interactWithoutAnimation = true;
        interactive.SetUser(user);
        interactive.LockForInteraction();
        interactive.Interact(interactionResult);
    }

    #endregion
}