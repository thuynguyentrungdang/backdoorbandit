using BepInEx;
using DoorBreach;
using DoorBreach.Fika;
using DoorBreachFika.Fika;
using DoorBreachFika.Patches;
using HarmonyLib;

namespace DoorBreachFika;

[BepInPlugin("com.dvize.backdoorbanditfika", "BackdoorBanditFika", "1.0.0")]
public class Plugin: BaseUnityPlugin
{
    public void Awake()
    {
        DoorBreachComponent.Logger.LogInfo("[BackdoorBanditFika] Plugin awoken.");
        
        FikaMethods.PluginEnabled();
        
        new RemoveItemFromPlayerInventoryPatch().Enable();
        
        FikaBridge.SendPlantC4PacketEmitted += FikaMethods.SendC4PlantPacket;
        FikaBridge.SendSyncOpenStatePacketEmitted += FikaMethods.SendSyncOpenStatePacket;
    }
}