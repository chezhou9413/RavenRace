using System;
using HarmonyLib;
using Verse;
using RavenRace.Compat.Wolfein;

namespace RavenRace.Features.Hybridization.Harmony
{
    /// <summary>
    /// 动态拦截沃芬的力量组件
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_WolfeinStrength
    {
        static Patch_WolfeinStrength()
        {
            if (!WolfeinCompatUtility.IsWolfeinActive) return;

            var harmony = new HarmonyLib.Harmony("ZuoYao.RavenRace.WolfeinSuppressor");
            Type targetType = AccessTools.TypeByName("Wolfein.CompWolfeinStrength");

            if (targetType != null)
            {
                var setupMethod = AccessTools.Method(targetType, "PostSpawnSetup");
                if (setupMethod != null)
                {
                    harmony.Patch(setupMethod, prefix: new HarmonyMethod(typeof(Patch_WolfeinStrength), nameof(Prefix_CancelSetup)));
                }
                RavenModUtility.LogVerbose("[RavenRace] Wolfein CompWolfeinStrength successfully suppressed for pure Ravens.");
            }
        }

        public static bool Prefix_CancelSetup(ThingComp __instance)
        {
            if (__instance.parent is Pawn pawn && pawn.def.defName == "Raven_Race")
            {
                if (!RavenRaceMod.Settings.enableWolfeinCompat) return false;
                if (!WolfeinCompatUtility.HasWolfeinBloodline(pawn)) return false;
            }
            return true;
        }
    }
}