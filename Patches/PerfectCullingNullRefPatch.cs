using System.Collections.Generic;
using System.Reflection;
using SPT.Reflection.Patching;
using Koenigz.PerfectCulling;
using Koenigz.PerfectCulling.EFT;
using UnityEngine;

namespace DoorBreach.Patches
{
    internal class PerfectCullingNullRefPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(PerfectCullingBakeGroup).GetMethod(nameof(PerfectCullingBakeGroup.Toggle));
        
        [PatchPrefix]
        private static bool Prefix(PerfectCullingBakeGroup __instance,
                                    ref int ___Int_0,
                                    PerfectCullingBakeGroup.RuntimeGroupContent[] ___RuntimeGroupContent_0,
                                    bool rendererEnabled)
        {
            if (__instance.runtimeProxies != null)
            {
                Renderer[] array = __instance.runtimeProxies;
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].enabled = !rendererEnabled;
                }
            }
            
            if (__instance.cullingLightObjects != null)
            {
                foreach (CullingObject cullingObject in __instance.cullingLightObjects)
                {
                    if (cullingObject != null)
                    {
                        cullingObject.SetAutocullVisibility(rendererEnabled);
                    }
                }
            }
            if (__instance.analyticSources != null)
            {
                foreach (AnalyticSource analyticSource in __instance.analyticSources)
                {
                    if (analyticSource != null)
                    {
                        analyticSource.IsAutocullVisible = rendererEnabled;
                    }
                }
            }
            if (__instance.screenDistanceSwitcher != null)
            {
                __instance.screenDistanceSwitcher.IsBakedAutocullVisible = rendererEnabled;
            }
            
            if (___Int_0 > ___RuntimeGroupContent_0.Length)
                ___Int_0 = ___RuntimeGroupContent_0.Length;
            
            for (int j = 0; j < ___Int_0; j++)
            {
                if (___RuntimeGroupContent_0[j].Renderer != null)
                    ___RuntimeGroupContent_0[j].Renderer.enabled = rendererEnabled;
            }
            
            return false;
        }
    }
}
