using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Buildings;

namespace RavenRace.Harmony
{
    /// <summary>
    /// 染色禁用扩展：标记建筑不接受 Stuff 染色。
    /// </summary>
    public class RavenStuffColorDisableExtension : DefModExtension
    {
        public bool disableStuffColor = true;
    }

    /// <summary>
    /// 渡鸦结构逻辑中心。
    /// 职责：劫持 DrawColor 确保贴图不发黑/不透明；劫持 SpawningWipes 确保墙体互盖。
    /// </summary>
    [HarmonyPatch]
    public static class RavenWallColorPatch
    {
        private static Dictionary<ThingDef, bool> cachedResults = new Dictionary<ThingDef, bool>();

        // 1. 颜色补丁
        [HarmonyPatch(typeof(Thing), "get_DrawColor")]
        [HarmonyPostfix]
        public static void DrawColorPostfix(Thing __instance, ref Color __result)
        {
            if (__instance.def == null) return;

            if (ShouldDisableStuffColor(__instance.def))
            {
                // 锁定为纯白，使渲染器直接显示 PNG 贴图原本的像素颜色
                __result = Color.white;
            }
        }

        private static bool ShouldDisableStuffColor(ThingDef def)
        {
            if (cachedResults.TryGetValue(def, out bool result)) return result;

            var ext = def.GetModExtension<RavenStuffColorDisableExtension>();
            bool disable = (ext != null && ext.disableStuffColor) || (def.defName == "Raven_Wall");

            cachedResults[def] = disable;
            return disable;
        }

        public static void ClearCache() => cachedResults.Clear();

        // 2. 覆盖建造补丁
        [HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
        [HarmonyPrefix]
        public static bool SpawningWipesPrefix(ref bool __result, BuildableDef newEntDef, BuildableDef oldEntDef)
        {
            ThingDef newDef = newEntDef as ThingDef;
            ThingDef oldDef = oldEntDef as ThingDef;

            if (newDef == null || oldDef == null) return true;

            ThingDef newBuiltDef = GenConstruct.BuiltDefOf(newDef) as ThingDef;
            ThingDef oldBuiltDef = GenConstruct.BuiltDefOf(oldDef) as ThingDef;

            if (newBuiltDef == null || oldBuiltDef == null) return true;

            bool newIsWall = newBuiltDef == ThingDefOf.Wall || newBuiltDef.thingClass == typeof(RavenWall_Building);
            bool oldIsWall = oldBuiltDef == ThingDefOf.Wall || oldBuiltDef.thingClass == typeof(RavenWall_Building);

            if (newIsWall && oldIsWall)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}