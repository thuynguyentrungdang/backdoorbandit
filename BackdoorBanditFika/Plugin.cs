using BepInEx;
using DoorBreach.Fika;
using DoorBreachFika.Fika;
using HarmonyLib;

namespace DoorBreachFika;

[BepInPlugin("com.dvize.backdoorbanditfika", "BackdoorBanditFika", "1.0.0")]
public class Plugin: BaseUnityPlugin
{
    public void Awake()
    {
        FikaMethods.PluginEnabled();
        FikaBridge.SendPlantC4PacketEmitted += FikaMethods.SendC4PlantPacket;
        FikaBridge.SendSyncOpenStatePacketEmitted += FikaMethods.SendSyncOpenStatePacket;

        Harmony harmony = new Harmony("DoorBreach");
        harmony.PatchAll();
    }
}