using DoorBreach.Fika;
using DoorBreachFika.Fika;
using DoorBreachFika.Patches;
using HarmonyLib;

namespace DoorBreachFika;

public class Plugin
{
    public static void Init()
    {
        FikaMethods.PluginEnabled();
        FikaBridge.SendPlantC4PacketEmitted += FikaMethods.SendC4PlantPacket;
        FikaBridge.SendSyncOpenStatePacketEmitted += FikaMethods.SendSyncOpenStatePacket;

        Harmony harmony = new Harmony("DoorBreach");
        harmony.PatchAll();
    }
}