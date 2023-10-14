using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reptile;
using HarmonyLib;

namespace WallPlant.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal static class PlayerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.InitAbilities))]
        private static void InitAbilities_Postfix(Player __instance, bool ___isAI)
        {
            if (___isAI)
                return;
            new WallPlantAbility(__instance);
            __instance.gameObject.AddComponent<WallPlantTrickHolder>();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.FixedUpdateAbilities))]
        private static void FixedUpdateAbilities_Prefix(Player __instance, bool ___isAI, Ability ___ability)
        {
            if (___isAI)
                return;
            if (___ability is WallPlantAbility)
                return;
            var wallPlantAbility = WallPlantAbility.Get(__instance);
            if (wallPlantAbility == null)
                return;
            wallPlantAbility.PassiveUpdate(___ability);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.RefreshAllDegrade))]
        private static void RefreshAllDegrade_Prefix(Player __instance, bool ___isAI)
        {
            if (___isAI)
                return;
            var trickHolder = WallPlantTrickHolder.Get(__instance);
            if (trickHolder == null)
                return;
            trickHolder.Refresh();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.RefreshAirTricks))]
        private static void RefreshAirTricks_Prefix(Player __instance, bool ___isAI)
        {
            if (___isAI)
                return;
            var trickHolder = WallPlantTrickHolder.Get(__instance);
            if (trickHolder == null)
                return;
            trickHolder.Refresh();
        }
    }
}
