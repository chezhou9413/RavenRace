using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;
using RavenRace.Compat.Moyo;

namespace RavenRace.Features.Hybridization.Harmony
{
    /// <summary>
    /// 动态拦截 Moyo 的产出组件
    /// 如果渡鸦没有血脉，则抑制该组件的更新和UI显示
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_MoyoResourceHarvestable
    {
        static Patch_MoyoResourceHarvestable()
        {
            if (!MoyoCompatUtility.IsMoyoActive) return;

            var harmony = new HarmonyLib.Harmony("ZuoYao.RavenRace.MoyoSuppressor");
            Type targetType = AccessTools.TypeByName("Moyo2_HPF.CompResourceHarvestable");

            if (targetType != null)
            {
                // 精准拦截 CompTickInterval
                var tickMethod = AccessTools.Method(targetType, "CompTickInterval");
                if (tickMethod != null)
                {
                    harmony.Patch(tickMethod, prefix: new HarmonyMethod(typeof(Patch_MoyoResourceHarvestable), nameof(Prefix_CancelIfInvalid)));
                }

                // 拦截 检查面板文本
                var inspectMethod = AccessTools.Method(targetType, "CompInspectStringExtra");
                if (inspectMethod != null)
                {
                    harmony.Patch(inspectMethod, prefix: new HarmonyMethod(typeof(Patch_MoyoResourceHarvestable), nameof(Prefix_CancelString)));
                }

                // 拦截 按钮
                var gizmoMethod = AccessTools.Method(targetType, "CompGetGizmosExtra");
                if (gizmoMethod != null)
                {
                    harmony.Patch(gizmoMethod, prefix: new HarmonyMethod(typeof(Patch_MoyoResourceHarvestable), nameof(Prefix_CancelGizmo)));
                }

                RavenModUtility.LogVerbose("[RavenRace] Moyo CompResourceHarvestable successfully suppressed for pure Ravens.");
            }
        }

        private static bool IsValid(ThingComp __instance)
        {
            if (__instance.parent is Pawn pawn && pawn.def.defName == "Raven_Race")
            {
                if (!RavenRaceMod.Settings.enableMoyoCompat) return false;
                if (!MoyoCompatUtility.HasMoyoBloodline(pawn)) return false;
            }
            return true;
        }

        public static bool Prefix_CancelIfInvalid(ThingComp __instance)
        {
            return IsValid(__instance);
        }

        public static bool Prefix_CancelString(ThingComp __instance, ref string __result)
        {
            if (!IsValid(__instance))
            {
                __result = null;
                return false;
            }
            return true;
        }

        public static bool Prefix_CancelGizmo(ThingComp __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!IsValid(__instance))
            {
                __result = Array.Empty<Gizmo>();
                return false;
            }
            return true;
        }
    }
}