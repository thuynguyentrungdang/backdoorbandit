using System;
using System.Reflection;
using System.Threading.Tasks;
using DoorBreach.Patches;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using DoorBreach.Models;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace DoorBreach
{
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)] 
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
        
        public static ModConfig ModConfig { get; set; }
        public static bool FikaInstalled { get; private set; }

        public enum GameObjectType
        {
            Door,
            Container,
            Trunk
        }

        private async void Awake()
        {
            FikaInstalled = Chainloader.PluginInfos.ContainsKey("com.fika.core");
            ModConfig = await LoadFromServer();

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

            new ApplyHit().Enable();
            new ActionMenuDoorPatch().Enable();
            new ActionMenuKeyCardPatch().Enable();
            new PerfectCullingNullRefPatch().Enable();
            new OnGameStartedPatch().Enable();

            TryInitFikaAssembly();
        }
        
        public void TryInitFikaAssembly()
        {
            if (!FikaInstalled)
                return;
            
            try
            {
                var fikaAssembly = Assembly.Load("BackdoorBanditFika");
                
                if (fikaAssembly == null) 
                    return;
                
                Type main = fikaAssembly.GetType("DoorBreachFika.Plugin");
                MethodInfo initMethod = main.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
                
                initMethod.Invoke(main, null);
                
                Logger.LogInfo("Fika assembly found, initialized Fika integration.");
            }
            catch (Exception)
            {
                Logger.LogInfo("Fika assembly not found, skipping Fika integration.");
            }
        }
        
        private static async Task<ModConfig> LoadFromServer()
        {
            try
            {
                string payload = await RequestHandler.GetJsonAsync("/backdoorbandit/load");
                
                return JsonConvert.DeserializeObject<ModConfig>(payload);
            }
            catch (Exception ex)
            {
                NotificationManagerClass.DisplayWarningNotification("Failed to load Backdoor Bandit server config - check the server");
                
                return null;
            }
        }
    }
}
