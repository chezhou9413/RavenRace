using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.CustomPawn.Binah
{
    // [核心修复] 改为 Patch StanceDraw，确保暂停时也能绘制，且不再闪烁
    [HarmonyPatch(typeof(Stance_Warmup), "StanceDraw")]
    public static class Patch_StanceWarmup_Visuals
    {
        [HarmonyPostfix]
        public static void Postfix(Stance_Warmup __instance)
        {
            // 基础检查
            if (__instance.verb == null || __instance.stanceTracker?.pawn == null) return;

            // 1. 柱之射击特效
            if (__instance.verb is Verb_BinahPillarBarrage pillarVerb)
            {
                pillarVerb.DrawWarmupEffect(__instance);
            }
            // 2. 震击特效
            else if (__instance.verb is Verb_BinahShockwave shockwaveVerb)
            {
                shockwaveVerb.DrawWarmupEffect(__instance);
            }
        }
    }
}