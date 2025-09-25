using System;
using System.Diagnostics;
using System.Reflection;
using SPT.Reflection.Patching;
using Comfort.Common;
using DoorBreach.Patches;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using EFT.Interactive;
using UnityEngine;
using Fika.Core.Networking;
using Fika.Core.Coop.Utils;
using Fika.Core.Coop.Players;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Coop.Components;
using LiteNetLib;
using LiteNetLib.Utils;

namespace DoorBreach
{
    [BepInPlugin("com.dvize.BackdoorBandit", "dvize.BackdoorBandit", "1.11.1")]
    public class DoorBreachPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PlebMode;
        public static ConfigEntry<bool> SemiPlebMode;
        public static ConfigEntry<bool> BreachingRoundsOpenMetalDoors;
        public static ConfigEntry<bool> OpenLootableContainers;
        public static ConfigEntry<bool> OpenCarDoors;
        public static ConfigEntry<int> MinHitPoints;
        public static ConfigEntry<int> MaxHitPoints;
        public static ConfigEntry<int> explosiveTimerInSec;
        public static ConfigEntry<bool> explosionDoesDamage;
        public static ConfigEntry<int> explosionRadius;
        public static ConfigEntry<int> explosionDamage;

        private readonly NetPacketProcessor packetProcessor = new NetPacketProcessor();

        public static int interactiveLayer;

        public enum GameObjectType
        {
            Door,
            Container,
            Trunk
        }

        private void Awake()
        {

            PlebMode = Config.Bind(
                "1. Main Settings",
                "Plebmode",
                false,
                new ConfigDescription("Enabled Means No Requirements To Breach Any Door/LootContainer",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 5 }));

            SemiPlebMode = Config.Bind(
                "1. Main Settings",
                "Semi-Plebmode",
                false,
                new ConfigDescription("Enabled Means Any Round Breach Regular Doors, Not Reinforced doors",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 4 }));

            BreachingRoundsOpenMetalDoors = Config.Bind(
                "1. Main Settings",
                "Breach Rounds Affects Metal Doors",
                false,
                new ConfigDescription("Enabled Means Any Breach Round opens a door",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 }));

            OpenLootableContainers = Config.Bind(
                "1. Main Settings",
                "Breach Lootable Containers",
                false,
                new ConfigDescription("If enabled, can use shotgun breach rounds on safes",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            OpenCarDoors = Config.Bind(
                "1. Main Settings",
                "Breach Car Doors",
                false,
                new ConfigDescription("If Enabled, can use shotgun breach rounds on car doors",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            MinHitPoints = Config.Bind(
                "2. Hit Points",
                "Min Hit Points",
                100,
                new ConfigDescription("Minimum Hit Points Required To Breach, Default 100",
                new AcceptableValueRange<int>(0, 1000),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            MaxHitPoints = Config.Bind(
                "2. Hit Points",
                "Max Hit Points",
                200,
                new ConfigDescription("Maximum Hit Points Required To Breach, Default 200",
                new AcceptableValueRange<int>(0, 2000),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            explosiveTimerInSec = Config.Bind(
                "3. Explosive",
                "Explosive Timer In Sec",
                10,
                new ConfigDescription("Time in seconds for explosive breach to detonate",
                new AcceptableValueRange<int>(1, 60),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 4 }));

            explosionDoesDamage = Config.Bind(
                "3. Explosive",
                "Enable Explosive Damage",
                false,
                new ConfigDescription("Enable damage from the explosive",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 }));

            explosionRadius = Config.Bind(
                "3. Explosive",
                "Explosion Radius",
                5,
                new ConfigDescription("Sets the radius for the explosion",
                new AcceptableValueRange<int>(0, 200),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            explosionDamage = Config.Bind(
               "3. Explosive",
               "Explosion Damage",
               80,
               new ConfigDescription("Amount of HP Damage the Explosion Causes",
               new AcceptableValueRange<int>(0, 500),
               new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            new NewGamePatch().Enable();
            new ApplyHit().Enable();
            new ActionMenuDoorPatch().Enable();
            new ActionMenuKeyCardPatch().Enable();
            new PerfectCullingNullRefPatch().Enable();

            FikaEventDispatcher.SubscribeEvent<GameWorldStartedEvent>(OnGameWorldStarted);
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreated);

            packetProcessor.SubscribeNetSerializable<PlantC4Packet, NetPeer>(OnTNTPacketReceived);
            packetProcessor.SubscribeNetSerializable<SyncOpenStatePacket, NetPeer>(OnSyncOpenStatePacketReceived);
        }

        void OnFikaNetworkManagerCreated (FikaNetworkManagerCreatedEvent ev)
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

        private void OnGameWorldStarted(GameWorldStartedEvent obj)
        {
            DoorBreachPlugin.interactiveLayer = LayerMask.NameToLayer("Interactive");

            DoorBreach.DoorBreachComponent.Enable();
            DoorBreach.ExplosiveBreachComponent.Enable();
        }

        private void OnTNTPacketReceived(PlantC4Packet packet, NetPeer peer)
        {
            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                if (coopHandler.Players.TryGetValue(packet.netID, out CoopPlayer player))
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
            {
                // If the host receives the packet from a client, now forward this packet to all clients (excluding arg2 - the person who sent it).
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            }
        }

        private void OnSyncOpenStatePacketReceived(SyncOpenStatePacket packet, NetPeer peer)
        {
            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                if (coopHandler.Players.TryGetValue(packet.netID, out CoopPlayer player))
                {
                    WorldInteractiveObject worldInteractiveObject = Singleton<GameWorld>.Instance.FindDoor(packet.objectID);
                    if (worldInteractiveObject != null && worldInteractiveObject.isActiveAndEnabled)
                    {
                        // Convert from int in the packet to the enum above
                        // (Can't send an enum value as part of a packet, apparently)
                        GameObjectType gameObjectType = (GameObjectType)packet.objectType;

                        switch (gameObjectType)
                        {
                            // Handle logic for ApplyHitPatch.OpenDoorIfNotAlreadyOpen on the recipient
                            case GameObjectType.Door:
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
                            case GameObjectType.Container:
                                {
                                    LootableContainer container = (LootableContainer)worldInteractiveObject;

                                    if (container.DoorState != EDoorState.Open)
                                    {
                                        container.DoorState = EDoorState.Shut;
                                        container.Open();
                                    }

                                    break;
                                }
                            case GameObjectType.Trunk:
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
                            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                        }
                    }
                }
            }
        }
    }



    //re-initializes each new game
    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix()
        {
            //stolen from drakiaxyz - thanks
            DoorBreachPlugin.interactiveLayer = LayerMask.NameToLayer("Interactive");

            DoorBreach.DoorBreachComponent.Enable();
            DoorBreach.ExplosiveBreachComponent.Enable();
        }
    }
}
