using Comfort.Common;
using DoorBreach;
using DoorBreachFika.Packets;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;

namespace DoorBreachFika.Fika;

public class FikaMethods
{
    public static void PluginEnabled()
    {
        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreated);
    }
    
    public static void SendC4PlantPacket(Player player, string doorID, int c4Timer)
    {
        FikaPlayer? fikaPlayer = player as FikaPlayer;

        if (fikaPlayer == null)
            return;
        
        PlantC4Packet packet = new PlantC4Packet
        {
            netID = fikaPlayer.Id,
            doorID = doorID,
            C4Timer = c4Timer,
        };

        if (FikaBackendUtils.IsServer)
            // Forward the packet to all clients
            Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            // ReliableOrdered = ensures the packet is received, re-sends it if it fails
        else if (FikaBackendUtils.IsClient)
            // If we're a client, send it to the host so they can forward it (Check Plugin.cs for behavior)
            Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    public static void SendSyncOpenStatePacket(Player player, string objectId, int objectType)
    {
        FikaPlayer? fikaPlayer = player as FikaPlayer;

        if (fikaPlayer == null)
            return;
        
        SyncOpenStatePacket packet = new SyncOpenStatePacket()
        {
            netID = fikaPlayer.Id,
            objectID = objectId,
            objectType = objectType
        };

        if (FikaBackendUtils.IsServer)
            Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        else if (FikaBackendUtils.IsClient)
            Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
    
    public static void OnFikaNetworkManagerCreated (FikaNetworkManagerCreatedEvent ev)
    {
        switch (ev.Manager)
        {
            case FikaServer server:
                server.RegisterPacket<PlantC4Packet, NetPeer>(OnTNTPacketReceived);
                server.RegisterPacket<SyncOpenStatePacket, NetPeer>(OnSyncOpenStatePacketReceived);                    
            break;
            case FikaClient client:
                client.RegisterPacket<PlantC4Packet, NetPeer>(OnTNTPacketReceived);
                client.RegisterPacket<SyncOpenStatePacket, NetPeer>(OnSyncOpenStatePacketReceived);
            break;
        }
    }

    private static void OnTNTPacketReceived(PlantC4Packet packet, NetPeer peer)
    {
        if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
        {
            if (coopHandler.Players.TryGetValue(packet.netID, out FikaPlayer player))
            {
                WorldInteractiveObject worldInteractiveObject = Singleton<GameWorld>.Instance.FindDoor(packet.doorID);
                
                if (worldInteractiveObject != null)
                {
                    // We can cast this to a Door since we're sure only a Door type was sent
                    Door door = (Door)worldInteractiveObject;

                    // Run the method on the recipient of this packet
                    ExplosiveBreachComponent.StartExplosiveBreach(door, player);
                }
            }
        }

        if (FikaBackendUtils.IsServer)
            // If the host receives the packet from a client, now forward this packet to all clients (excluding arg2 - the person who sent it).
            Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
    }

    private static void OnSyncOpenStatePacketReceived(SyncOpenStatePacket packet, NetPeer peer)
    {
        if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler) ||
            !coopHandler.Players.TryGetValue(packet.netID, out _)) 
            return;
            
        WorldInteractiveObject worldInteractiveObject = Singleton<GameWorld>.Instance.FindDoor(packet.objectID);

        if (worldInteractiveObject == null || 
            !worldInteractiveObject.isActiveAndEnabled) 
            return;
            
        // Convert from int in the packet to the enum above
        // (Can't send an enum value as part of a packet, apparently)
        DoorBreachPlugin.GameObjectType gameObjectType = (DoorBreachPlugin.GameObjectType)packet.objectType;

        switch (gameObjectType)
        {
            // Handle logic for ApplyHitPatch.OpenDoorIfNotAlreadyOpen on the recipient
            case DoorBreachPlugin.GameObjectType.Door:
            {
                Door door = (Door)worldInteractiveObject;

                if (door.DoorState != EDoorState.Open)
                {
                    door.DoorState = EDoorState.Shut;
                    //player.CurrentManagedState.ExecuteDoorInteraction(container, new InteractionResult(EInteractionType.Breach), null, player);
                    door.KickOpen(true);
                    coopHandler.MyPlayer.UpdateInteractionCast();
                }
                break;
            }
            case DoorBreachPlugin.GameObjectType.Container:
            {
                LootableContainer container = (LootableContainer)worldInteractiveObject;

                if (container.DoorState != EDoorState.Open)
                {
                    container.DoorState = EDoorState.Shut;
                    container.Open();
                }

                break;
            }
            case DoorBreachPlugin.GameObjectType.Trunk:
            {
                Trunk trunk = (Trunk)worldInteractiveObject;

                if (trunk.DoorState != EDoorState.Open)
                {
                    trunk.DoorState = EDoorState.Shut;
                    trunk.Open();
                }

                break;
            }
        }

        if (FikaBackendUtils.IsServer)
        {
            Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }
    }
}