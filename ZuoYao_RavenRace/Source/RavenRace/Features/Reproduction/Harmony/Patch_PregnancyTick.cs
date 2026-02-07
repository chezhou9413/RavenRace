using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RavenRace.Features.Reproduction.Harmony
{
    [HarmonyPatch(typeof(Hediff_Pregnant), "TickInterval")]
    public static class Patch_PregnancyTick
    {
        [HarmonyPrefix]
        public static bool Prefix(Hediff_Pregnant __instance, int delta)
        {
            if (!RavenRaceMod.Settings.ignoreFertilityForPregnancy) return true;

            bool isMalePregnancy = __instance.pawn.gender == Gender.Male;
            bool isRavenInvolved = __instance.pawn.def == RavenDefOf.Raven_Race ||
                                   (__instance.Father != null && __instance.Father.def == RavenDefOf.Raven_Race);

            if (isMalePregnancy || isRavenInvolved)
            {
                float growthSpeed = PawnUtility.BodyResourceGrowthSpeed(__instance.pawn);
                float gestationPeriod = __instance.pawn.RaceProps.gestationPeriodDays * 60000f;
                if (gestationPeriod <= 0) gestationPeriod = 45f * 60000f;

                float progressChange = growthSpeed / gestationPeriod * delta;
                __instance.Severity += progressChange;

                if (__instance.Severity >= 1f)
                {
                    if (isMalePregnancy)
                    {
                        if (!__instance.pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLabor))
                        {
                            __instance.pawn.health.AddHediff(HediffDefOf.PregnancyLabor);
                        }
                    }
                    else
                    {
                        if (!__instance.pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLabor))
                        {
                            __instance.pawn.health.AddHediff(HediffDefOf.PregnancyLabor);
                        }
                    }
                    __instance.pawn.health.RemoveHediff(__instance);
                }
                return false;
            }
            return true;
        }
    }
}