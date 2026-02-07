using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RavenRace.Features.DegradationCharm.Harmony
{
    [HarmonyPatch]
    public static class Patch_LovinHooks
    {
        [HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
        [HarmonyPostfix]
        public static IEnumerable<Toil> Lovin_Postfix(IEnumerable<Toil> values, JobDriver_Lovin __instance)
        {
            Pawn actor = __instance.pawn;
            Pawn partner = (Pawn)__instance.job.GetTarget(TargetIndex.A).Thing;

            foreach (var toil in values)
            {
                toil.AddFinishAction(() => {
                    IncreaseDegradation(actor);
                    IncreaseDegradation(partner);
                });
                yield return toil;
            }
        }

        // [最终修正] 根据您的最新代码，JobDriver_ForceLovin 确实在顶层命名空间
        [HarmonyPatch(typeof(RavenRace.JobDriver_ForceLovin), "MakeNewToils")]
        [HarmonyPostfix]
        public static IEnumerable<Toil> ForceLovin_Postfix(IEnumerable<Toil> values, JobDriver_ForceLovin __instance)
        {
            Pawn actor = __instance.pawn;
            Pawn partner = (Pawn)__instance.job.GetTarget(TargetIndex.A).Thing;

            foreach (var toil in values)
            {
                toil.AddFinishAction(() => {
                    IncreaseDegradation(actor);
                    IncreaseDegradation(partner);
                });
                yield return toil;
            }
        }

        private static void IncreaseDegradation(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DegradationCharmDefOf.Raven_Hediff_Degradation);
            if (hediff != null)
            {
                hediff.Severity += 0.10f;
            }
        }

        [HarmonyPatch(typeof(JobDriver_Lovin), "GenerateRandomMinTicksToNextLovin")]
        [HarmonyPostfix]
        public static void ModifyLovinInterval_Postfix(Pawn pawn, ref int __result)
        {
            if (pawn.story?.traits?.HasTrait(DegradationCharmDefOf.Raven_Trait_Lecherous) ?? false)
            {
                __result /= 10;
            }
        }
    }
}