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
        /*static bool Prefix(PerfectCullingBakeGroup __instance, bool rendererEnabled, int ___Int0, PerfectCullingBakeGroup.RuntimeGroupContent[] ___RuntimeGroupContent_0)
        {
            // Safely handle Renderer[] array
            if (__instance.runtimeProxies != null)
            {
                foreach (Renderer renderer in __instance.runtimeProxies)
                {
                    if (renderer != null)
                        renderer.enabled = !rendererEnabled;
                }
            }

            // Safely handle CullingObject array
            if (__instance.cullingLightObjects != null)
            {
                foreach (CullingObject cullingObject in __instance.cullingLightObjects)
                {
                    if (cullingObject != null)
                        cullingObject.SetAutocullVisibility(rendererEnabled);
                }
            }

            // Safely handle AnalyticSource array
            if (__instance.analyticSources != null)
            {
                foreach (AnalyticSource analyticSource in __instance.analyticSources)
                {
                    if (analyticSource != null)
                        analyticSource.IsAutocullVisible = rendererEnabled;
                }
            }

            // Safely handle ScreenDistanceSwitcher
            if (__instance.screenDistanceSwitcher != null)
                __instance.screenDistanceSwitcher.IsBakedAutocullVisible = rendererEnabled;

            // Safely handle RuntimeGroupContent
            for (int j = 0; j < ___Int0; j++)
            {
                if (___RuntimeGroupContent_0[j].Renderer != null)
                    ___RuntimeGroupContent_0[j].Renderer.enabled = rendererEnabled;
            }

            return false; // Skip original method execution
        }*/
        
        [HarmonyPrefix]
        static bool Prefix(
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
