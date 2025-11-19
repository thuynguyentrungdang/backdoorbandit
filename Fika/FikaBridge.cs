using EFT;

namespace DoorBreach.Fika;

public class FikaBridge
{
    public delegate void SimpleEvent();
    public static event SimpleEvent PluginEnableEmitted;
    public static void PluginEnable()
    {
        PluginEnableEmitted?.Invoke(); 
    }
    
    public delegate void SendPlantC4PacketEvent(int playerId, string doorId, int c4Timer);
    public static event SendPlantC4PacketEvent SendPlantC4PacketEmitted;
    public static void SendPlantC4PacketPacket(Player player, string doorId, int c4Timer)
    { 
        //Plugin.LogSource.LogDebug("Sending player position packet");
        SendPlantC4PacketEmitted?.Invoke(player.Id, doorId, c4Timer); 
    }
    
    public delegate void SendSyncOpenStatePacketEvent(int playerId, string objectId, int objectType);
    public static event SendSyncOpenStatePacketEvent SendSyncOpenStatePacketEmitted;
    public static void SendSyncOpenStatePacket(Player player, string objectId, int objectType)
    { 
        //Plugin.LogSource.LogDebug("Sending player position packet");
        SendSyncOpenStatePacketEmitted?.Invoke(player.Id, objectId, objectType); 
    }
}