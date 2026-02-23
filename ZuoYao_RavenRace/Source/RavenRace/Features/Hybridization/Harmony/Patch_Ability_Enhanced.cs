using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using RavenRace.Compat.Epona;
using RavenRace.Compat.MuGirl;

namespace RavenRace.Features.Hybridization.Harmony
{
    /// <summary>
    /// 优化后的雪牛娘与艾波娜联动能力补丁。
    /// 负责在渡鸦同时拥有两种血脉时，强化“雪牛冲锋”的距离和UI描述。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_Ability_Enhanced
    {
        static Patch_Ability_Enhanced()
        {
            // 如果雪牛娘未加载，直接跳过所有补丁
            if (!MuGirlCompatUtility.IsMuGirlActive) return;

            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("ZuoYao.RavenRace.MuGirlEponaAbility");

            // 1. 拦截能力按钮的生成
            var originalGizmo = AccessTools.Method(typeof(Ability), "GetGizmos");
            var postfixGizmo = AccessTools.Method(typeof(Patch_Ability_Enhanced), nameof(Postfix_GetGizmos));
            if (originalGizmo != null && postfixGizmo != null)
            {
                harmony.Patch(originalGizmo, postfix: new HarmonyMethod(postfixGizmo));
            }

            // 2. 拦截能力射程的获取
            var originalRange = AccessTools.PropertyGetter(typeof(Verb), "Range");
            var postfixRange = AccessTools.Method(typeof(Patch_Ability_Enhanced), nameof(Postfix_GetRange));
            if (originalRange != null && postfixRange != null)
            {
                harmony.Patch(originalRange, postfix: new HarmonyMethod(postfixRange));
            }

            RavenModUtility.LogVerbose("[RavenRace] Epona & MuGirl Enhanced Ability Patch applied safely.");
        }

        /// <summary>
        /// 修改技能图标的描述和名称
        /// </summary>
        public static IEnumerable<Gizmo> Postfix_GetGizmos(IEnumerable<Gizmo> __result, Ability __instance)
        {
            // 【核心优化】：如果不是我们的雪牛冲锋，或者不满足强化条件，直接原样返回，没有任何性能消耗！
            if (__instance.def.defName != "Raven_Ability_MuGirlCharge" ||
                !RavenRaceMod.Settings.enableMuGirlCompat ||
                !EponaCompatUtility.IsEponaActive ||
                !EponaCompatUtility.HasEponaBloodline(__instance.pawn))
            {
                return __result;
            }

            // 只有满足条件，才走修改逻辑
            return ModifyGizmo(__result);
        }

        private static IEnumerable<Gizmo> ModifyGizmo(IEnumerable<Gizmo> gizmos)
        {
            foreach (var gizmo in gizmos)
            {
                if (gizmo is Command_Ability cmd)
                {
                    cmd.defaultLabel = "RavenRace_AbilityLabel_EnhancedCharge".Translate();
                    cmd.defaultDesc = "RavenRace_AbilityDesc_EnhancedCharge".Translate();
                }
                yield return gizmo;
            }
        }

        /// <summary>
        /// 修改技能的射程
        /// </summary>
        public static void Postfix_GetRange(Verb __instance, ref float __result)
        {
            if (__instance is Verb_CastAbility castVerb && castVerb.ability != null)
            {
                if (castVerb.ability.def.defName == "Raven_Ability_MuGirlCharge")
                {
                    Pawn pawn = castVerb.CasterPawn;
                    if (pawn != null &&
                        RavenRaceMod.Settings.enableMuGirlCompat &&
                        EponaCompatUtility.IsEponaActive &&
                        EponaCompatUtility.HasEponaBloodline(pawn))
                    {
                        __result = 35.9f; // 艾波娜强化后的超远距离
                    }
                }
            }
        }
    }
}