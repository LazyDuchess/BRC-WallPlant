using HarmonyLib;
using Reptile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This is very annoying, but we have to inject code into all these abilities so that we can cancel them with wall plants. This is how air dashing and other vanilla abilities do it too.
namespace WallPlant.Patches
{
    [HarmonyPatch(typeof(AirTrickAbility), "FixedUpdateAbility")]
    internal static class AirTrickAbilityPatch
    {
        private static bool Prefix(Ability __instance)
        {
            return AbilityPatches.Prefix(__instance);
        }
    }
    [HarmonyPatch(typeof(BoostAbility), "FixedUpdateAbility")]
    internal static class BoostAbilityPatch
    {
        private static bool Prefix(Ability __instance)
        {
            return AbilityPatches.Prefix(__instance);
        }
    }
    [HarmonyPatch(typeof(FlipOutJumpAbility), "FixedUpdateAbility")]
    internal static class FlipOutJumpAbilityPatch
    {
        private static bool Prefix(Ability __instance)
        {
            return AbilityPatches.Prefix(__instance);
        }
    }
    [HarmonyPatch(typeof(HitBounceAbility), "FixedUpdateAbility")]
    internal static class HitBounceAbilityPatch
    {
        private static bool Prefix(Ability __instance)
        {
            return AbilityPatches.Prefix(__instance);
        }
    }
    [HarmonyPatch(typeof(SpecialAirAbility), "FixedUpdateAbility")]
    internal static class SpecialAirAbilityPatch
    {
        private static bool Prefix(Ability __instance)
        {
            return AbilityPatches.Prefix(__instance);
        }
    }
    [HarmonyPatch(typeof(AirDashAbility), "FixedUpdateAbility")]
    internal static class AirDashAbilityPatch
    {
        private static bool Prefix(Ability __instance)
        {
            return AbilityPatches.Prefix(__instance);
        }
    }
    internal static class AbilityPatches
    {
        internal static bool Prefix(Ability __instance)
        {
            var wallPlantAbility = WallPlantAbility.Get(__instance.p);
            if (wallPlantAbility == null)
                return true;

            var minTimeForAbility = 0f;

            if (__instance is FlipOutJumpAbility)
                minTimeForAbility = 0.25f;
            else if (__instance is HitBounceAbility)
                minTimeForAbility = 0.5f;
            else if (__instance is SpecialAirAbility)
            {
                var airAbility = __instance as SpecialAirAbility;
                minTimeForAbility = airAbility.duration - (airAbility.hitEnemy ? 1.2f : 0.35f);
            }
            else if (__instance is VertAbility)
            {
                minTimeForAbility = 0.1f;
            }
            else if (__instance is AirDashAbility)
            {
                minTimeForAbility = 0.1f;
            }

            if (minTimeForAbility != 0f)
            {
                if (__instance.p.abilityTimer <= minTimeForAbility)
                    return true;
            }

            if (wallPlantAbility.CheckActivation())
                return false;
            return true;
        }
    }
}
