using System.Collections.Generic;

namespace DoorBreach.Models;

public class ModConfig
{
    public bool Enabled { get; set; }
    public List<string> ShotgunList { get; set; }
    public List<string> GrenadeLauncherList { get; set; }
    public List<string> MeleeWeaponList { get; set; }
    public List<string> OtherWeaponList { get; set; }
    public List<string> BlacklistedRooms { get; set; }
    public List<string> WhitelistedRounds { get; set; }
    public bool PlebMode { get; set; }
    public bool SemiPlebMode { get; set; }
    public bool BreachingRoundsOpenMetalDoors { get; set; }
    public bool OpenLootableContainers { get; set; }
    public bool OpenCarDoors { get; set; }
    public int MinHitPoints { get; set; }
    public int MaxHitPoints { get; set; }
    public int ExplosiveTimerInSec { get; set; }
    public ExplosionStats ModExplosionStats = new();
    public class ExplosionStats
    {
        public bool ExplosionDoesDamage { get; set; }
        public int ExplosionRadius { get; set; }
        public int ExplosionDamage { get; set; }
    }
}