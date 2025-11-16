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
    private static void PatchPrefix(Player ___player, string ___C4ExplosiveId)
    {
        IEnumerable<Item> items = ___player.Inventory.GetPlayerItems(EPlayerItems.Equipment);
        Item foundItem = null;

        foreach (var item in items)
        {
            if (item.TemplateId != ___C4ExplosiveId) 
                continue;
                
            foundItem = item;
            break;
        }
            
        FikaPlayer coopPlayer = ___player as FikaPlayer;

        if (coopPlayer == null)
            return;
            
        InventoryController inventoryController = coopPlayer.InventoryController;

        DoorBreachComponent.Logger.LogInfo($"Attempting to remove C4 from player inventory. Player ID: {coopPlayer.NetId}");

        if (foundItem == null) 
            return;

        DoorBreachComponent.Logger.LogInfo($"Removing C4 with ID: {foundItem?.Id} from player inventory.");

        GStruct153 discardResult = InteractionsHandlerClass.Discard(foundItem, inventoryController, true);

        if (discardResult.Failed)
            return;

        inventoryController.TryRunNetworkTransaction(discardResult, null);
    }

    
}