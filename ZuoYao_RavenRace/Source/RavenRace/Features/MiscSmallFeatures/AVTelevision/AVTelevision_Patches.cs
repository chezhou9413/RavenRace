using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RavenRace.Features.MiscSmallFeatures.AVTelevision
{
    public static class AVTelevision_Patches
    {
        private static JoyKindDef adultJoyKind;
        private static JoyKindDef AdultJoyKind => adultJoyKind ?? (adultJoyKind = DefDatabase<JoyKindDef>.GetNamed("Raven_AdultEntertainment", false));

        // ==========================================
        // 1. 暴力劫持吸引力：当权重设为 100% 时，所有人都会跑来看电视
        // ==========================================
        [HarmonyPatch(typeof(JoyGiver), nameof(JoyGiver.GetChance))]
        public static class Patch_ExtremeGiverChance
        {
            [HarmonyPostfix]
            public static void Postfix(JoyGiver __instance, ref float __result)
            {
                if (__instance.def.defName == "Raven_Giver_WatchAV")
                {
                    __result *= RavenRaceMod.Settings.avJoyWeightMultiplier;
                }
                else if (RavenRaceMod.Settings.avJoyWeightMultiplier >= 99f)
                {
                    // 100% 模式：屏蔽其他所有娱乐
                    __result = 0f;
                }
            }
        }

        // ==========================================
        // 2. 彻底屏蔽 JoyKind 不匹配红字 (1.6 闭环)
        // ==========================================
        [HarmonyPatch(typeof(JoyUtility), nameof(JoyUtility.JoyTickCheckEnd))]
        public static class Patch_JoyTick_SystemBypass
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn pawn, int delta, ref JoyTickFullJoyAction fullJoyAction, ref float extraJoyGainFactor, ref Building joySource)
            {
                // 如果当前正在看渡鸦成人频道，手动接管计算
                if (pawn.CurJob != null && pawn.CurJob.def.defName == "Raven_WatchAV")
                {
                    Need_Joy joy = pawn.needs.joy;
                    if (joy != null)
                    {
                        float amount = extraJoyGainFactor * pawn.CurJob.def.joyGainRate * 0.36f / 2500f * (float)delta;
                        joy.GainJoy(amount, pawn.CurJob.def.joyKind);

                        if (joy.CurLevel > 0.9999f && !pawn.CurJob.doUntilGatheringEnded)
                        {
                            if (fullJoyAction == JoyTickFullJoyAction.EndJob) pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                            else if (fullJoyAction == JoyTickFullJoyAction.GoToNextToil) pawn.jobs.curDriver.ReadyForNextToil();
                        }
                    }
                    return false; // 拦截并阻止执行原版那个会报 ErrorOnce 的 mismatch 检查
                }
                return true;
            }
        }

        // ==========================================
        // 3. 拦截耐受度索引器 (保持 100% 新鲜感)
        // ==========================================
        [HarmonyPatch(typeof(JoyToleranceSet), "get_Item")]
        public static class Patch_JoyTolerance_Getter
        {
            [HarmonyPrefix]
            public static bool Prefix(JoyKindDef d, ref float __result)
            {
                if (RavenRaceMod.Settings.avDisableTolerance && d != null && AdultJoyKind != null && d == AdultJoyKind)
                {
                    __result = 0f;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JoyToleranceSet), nameof(JoyToleranceSet.Notify_JoyGained))]
        public static class Patch_JoyTolerance_NoGain
        {
            [HarmonyPrefix]
            public static bool Prefix(JoyKindDef joyKind)
            {
                if (RavenRaceMod.Settings.avDisableTolerance && joyKind != null && AdultJoyKind != null && joyKind == AdultJoyKind)
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JoyToleranceSet), nameof(JoyToleranceSet.BoredOf))]
        public static class Patch_JoyTolerance_NeverBored
        {
            [HarmonyPrefix]
            public static bool Prefix(JoyKindDef def, ref bool __result)
            {
                if (RavenRaceMod.Settings.avDisableTolerance && def != null && AdultJoyKind != null && def == AdultJoyKind)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        // ==========================================
        // 4. 交配逻辑钩子：修复权限
        // ==========================================
        [HarmonyPatch(typeof(JobDriver_WatchTelevision), "WatchTickAction")]
        public static class Patch_MatingHook
        {
            [HarmonyPostfix]
            public static void Postfix(JobDriver_WatchTelevision __instance)
            {
                if (__instance.job == null) return;
                Pawn pawn = __instance.pawn;

                if (pawn.IsHashIntervalTick(250))
                {
                    var tv = __instance.job.targetA.Thing;
                    var comp = tv?.TryGetComp<CompTV_AV>();
                    if (comp != null && comp.avModeActive)
                    {
                        comp.Notify_PawnWatching(pawn);
                    }
                }
            }
        }
    }
}