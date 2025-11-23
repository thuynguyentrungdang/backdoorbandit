using System;
using System.Threading.Tasks;
using DoorBreach.Patches;
using BepInEx;
using DoorBreach.Models;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace DoorBreach
{
    [BepInPlugin("com.dvize.BackdoorBandit", "dvize.BackdoorBandit", "2.0.1")]
    public class DoorBreachPlugin : BaseUnityPlugin
    {
        public static bool PlebMode;
        public static bool SemiPlebMode;
        public static bool BreachingRoundsOpenMetalDoors;
        public static bool OpenLootableContainers;
        public static bool OpenCarDoors;
        public static int MinHitPoints;
        public static int MaxHitPoints;
        public static int explosiveTimerInSec;
        public static bool explosionDoesDamage;
        public static int explosionRadius;
        public static int explosionDamage;
        
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
            ModConfig = await LoadFromServer();

            PlebMode = ModConfig.PlebMode;

            SemiPlebMode = ModConfig.SemiPlebMode;

            BreachingRoundsOpenMetalDoors = ModConfig.BreachingRoundsOpenMetalDoors;

            OpenLootableContainers = ModConfig.OpenLootableContainers;

            OpenCarDoors = ModConfig.OpenCarDoors;

            MinHitPoints = ModConfig.MinHitPoints;

            MaxHitPoints = ModConfig.MaxHitPoints;

            explosiveTimerInSec = ModConfig.ExplosiveTimerInSec;

            explosionDoesDamage = ModConfig.ModExplosionStats.ExplosionDoesDamage;

            explosionRadius = ModConfig.ModExplosionStats.ExplosionRadius;

            explosionDamage = ModConfig.ModExplosionStats.ExplosionDamage;

            new ApplyHit().Enable();
            new ActionMenuDoorPatch().Enable();
            new ActionMenuKeyCardPatch().Enable();
            new PerfectCullingNullRefPatch().Enable();
            new OnGameStartedPatch().Enable();
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
