using System;
using HarmonyLib;
using Verse;
using RimWorld;
using RavenRace.Compat.Moyo;

namespace RavenRace.Features.Hybridization.Harmony
{
    /// <summary>
    /// 处理 Moyo 深蓝成瘾免疫逻辑
    /// </summary>
    [HarmonyPatch]
    public static class Patch_MoyoImmunity
    {
        // 1. 拦截 Hediff 添加，如果是深蓝相关且为混血渡鸦，则阻止添加
        // 注意：AddHediff 的参数必须与原版方法签名完全匹配，或者使用 Harmony 的特殊参数
        [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
        [HarmonyPrefix]
        public static bool PreventDeepBlueAddiction(Pawn_HealthTracker __instance, Hediff hediff, Pawn ___pawn)
        {
            // 如果 Moyo 未加载，直接跳过
            if (!MoyoCompatUtility.IsMoyoActive) return true;

            if (hediff == null || hediff.def == null) return true;

            // 检查 DefName (硬编码检查，因为我们没有引用 Moyo DLL)
            string defName = hediff.def.defName;
            if (defName == "DeepBlueTolerance" || defName == "DeepBlueAddiction")
            {
                // 使用 Harmony 注入的 ___pawn 私有字段
                Pawn pawn = ___pawn;

                // 双重保险：如果注入失败 (极少见)，尝试通过 hediffSet 获取 (它是 public 的)
                if (pawn == null && __instance.hediffSet != null)
                    pawn = __instance.hediffSet.pawn;

                if (pawn != null && pawn.def.defName == "Raven_Race")
                {
                    // 检查血脉
                    if (RavenRaceMod.Settings.enableMoyoCompat && MoyoCompatUtility.HasMoyoBloodline(pawn))
                    {
                        // 混血渡鸦免疫深蓝成瘾和耐受，直接拦截 (返回 false)
                        return false;
                    }
                }
            }
            return true; // 继续执行原版逻辑
        }
    }
}