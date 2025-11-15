using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace DoorBreach.Patches;

public class OnGameStartedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    private static void PatchPostfix()
    {
        if (Application.isBatchMode)
            return;
        
        DoorBreachComponent.Enable();
        ExplosiveBreachComponent.Enable();
    }
}