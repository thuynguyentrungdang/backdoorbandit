using BepInEx;
using BepInEx.Logging;
using DoorBreach;
using DoorBreach.Fika;
using DoorBreachFika.Fika;
using DoorBreachFika.Patches;
using HarmonyLib;

namespace DoorBreachFika;

[BepInPlugin("com.dvize.backdoorbanditfika", "BackdoorBanditFika", "1.0.0")]
public class Plugin: BaseUnityPlugin
{
    public static ManualLogSource Logger
    {
        get; private set;
    }
    
    public void Awake()
    {
        Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(Plugin));
        
        FikaMethods.PluginEnabled();
        
        new RemoveItemFromPlayerInventoryPatch().Enable();
        
        FikaBridge.SendPlantC4PacketEmitted += FikaMethods.SendC4PlantPacket;
        FikaBridge.SendSyncOpenStatePacketEmitted += FikaMethods.SendSyncOpenStatePacket;
    }
}