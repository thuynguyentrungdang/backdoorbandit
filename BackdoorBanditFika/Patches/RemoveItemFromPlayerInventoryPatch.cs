using System;
using System.Collections.Generic;
using System.Reflection;
using DoorBreach;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DoorBreachFika.Patches;

public class RemoveItemFromPlayerInventoryPatch: ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ExplosiveBreachComponent), nameof(ExplosiveBreachComponent.RemoveItemFromPlayerInventory));
    }
    
    [PatchPrefix]
    private static bool PatchPrefix(Player ___player, string ___C4ExplosiveId)
    {
        DoorBreachComponent.Logger.LogInfo("[BackdoorBanditFika] RemoveItemFromPlayerInventoryPatch called.");

        if (___player is not FikaPlayer fikaPlayer)
        {
            DoorBreachComponent.Logger.LogError("[BackdoorBanditFika] Player is not a FikaPlayer.");
            return true;
        }
        
        IEnumerable<Item> items = fikaPlayer.Inventory.GetPlayerItems(EPlayerItems.Equipment);
        Item foundItem = null;

        foreach (var item in items)
        {
            if (item.TemplateId != ___C4ExplosiveId) 
                continue;
                
            foundItem = item;
            break;
        }
        
        if (fikaPlayer == null)
        {
            DoorBreachComponent.Logger.LogError("[BackdoorBanditFika] fikaPlayer is null.");
            return true;
        }

        InventoryController inventoryController = fikaPlayer.InventoryController;

        DoorBreachComponent.Logger.LogInfo($"Attempting to remove C4 from player inventory. Player ID: {fikaPlayer.NetId}");

        if (foundItem == null)
        {
            DoorBreachComponent.Logger.LogError("[BackdoorBanditFika] foundItem is null.");
            return true;
        }

        DoorBreachComponent.Logger.LogInfo($"Removing C4 with ID: {foundItem?.Id} from player inventory.");

        GStruct153 discardResult = InteractionsHandlerClass.Discard(foundItem, inventoryController, true);

        if (discardResult.Failed)
        {
            DoorBreachComponent.Logger.LogError("[BackdoorBanditFika] discardResult failed.");
            return true;
        }

        inventoryController.TryRunNetworkTransaction(discardResult);
        
        return false;
    }
}