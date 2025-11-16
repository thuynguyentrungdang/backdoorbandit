using System.Linq;
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using Koenigz.PerfectCulling;
using UnityEngine;

namespace DoorBreach.Patches
{
    internal class PerfectCullingNullRefPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(PerfectCullingBakeGroup).GetMethod(nameof(PerfectCullingBakeGroup.Toggle));
        
        [PatchPrefix]
        private static bool Prefix(
            PerfectCullingBakeGroup __instance,
            ref int ___Int_0,
            ref PerfectCullingBakeGroup.RuntimeGroupContent[] ___RuntimeGroupContent_0
        )
        {
            // Null-safety: replace null array with empty
            if (___RuntimeGroupContent_0 == null)
            {
                ___RuntimeGroupContent_0 = [];
                ___Int_0 = 0;
            }

            // Clamp Int0 to avoid out-of-bounds
            if (___Int_0 > ___RuntimeGroupContent_0.Length)
                ___Int_0 = ___RuntimeGroupContent_0.Length;

            // Original method runs safely
            return true;
        }
    }
}
