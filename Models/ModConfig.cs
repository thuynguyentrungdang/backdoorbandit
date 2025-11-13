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
}