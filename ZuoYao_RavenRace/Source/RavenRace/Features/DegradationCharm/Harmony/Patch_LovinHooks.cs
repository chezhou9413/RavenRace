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
            // [安全修正] 使用 as 转换避免崩溃
            Pawn partner = __instance.job.GetTarget(TargetIndex.A).Thing as Pawn;

            foreach (var toil in values)
            {
                toil.AddFinishAction(() => {
                    IncreaseDegradation(actor);
                    // 仅当对象确实是生物时才增加其淫堕值
                    if (partner != null)
                    {
                        IncreaseDegradation(partner);
                    }
                });
                yield return toil;
            }
        }

        // [最终修正] 拦截渡鸦族的强制求爱工作
        [HarmonyPatch(typeof(RavenRace.JobDriver_ForceLovin), "MakeNewToils")]
        [HarmonyPostfix]
        public static IEnumerable<Toil> ForceLovin_Postfix(IEnumerable<Toil> values, JobDriver_ForceLovin __instance)
        {
            Pawn actor = __instance.pawn;
            // [核心修复] 使用 as 安全转换。如果目标是建筑(墙体)，这里将安全地返回 null 而不是引发 InvalidCastException 崩溃
            Pawn partner = __instance.job.GetTarget(TargetIndex.A).Thing as Pawn;

            foreach (var toil in values)
            {
                toil.AddFinishAction(() => {
                    IncreaseDegradation(actor);
                    // 仅当交互对象是 Pawn 时才增加其淫堕值（放过墙体）
                    if (partner != null)
                    {
                        IncreaseDegradation(partner);
                    }
                });
                yield return toil;
            }
        }

        /// <summary>
        /// 增加指定 Pawn 身上淫堕符咒状态的严重度
        /// </summary>
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
            // 如果带有“淫堕狂宴”特性，则大大缩短下一次交配的间隔（除以10）
            if (pawn.story?.traits?.HasTrait(DegradationCharmDefOf.Raven_Trait_Lecherous) ?? false)
            {
                __result /= 10;
            }
        }
    }
}