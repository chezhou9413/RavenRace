using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Buildings;

namespace RavenRace.Core.Harmony
{
    /// <summary>
    /// Mod扩展类，用于在XML中标记一个建筑是否应该禁用材料染色。
    /// </summary>
    public class RavenStuffColorDisableExtension : DefModExtension
    {
        public bool disableStuffColor = true;
    }

    /// <summary>
    /// 渡鸦族墙体及其他禁用染色建筑的视觉和行为补丁中心。
    /// 职责：
    /// 1. 劫持 DrawColor 属性，强制返回白色，确保贴图使用其原始颜色，不受建造材料（如钢铁的灰色）的影响。
    /// 2. 劫持 SpawningWipes 逻辑，确保渡鸦墙体可以像原版墙体一样相互替换建造，而不是提示“空间被占用”。
    /// </summary>
    [HarmonyPatch]
    public static class RavenWallColorPatch
    {
        // 缓存字典，用于存储每个ThingDef是否需要禁用染色的结果，避免每次都读取ModExtension，提高性能。
        private static Dictionary<ThingDef, bool> cachedResults = new Dictionary<ThingDef, bool>();

        /// <summary>
        /// 补丁目标：Thing.DrawColor 属性的 get 方法。
        /// 在原版方法执行后，检查是否需要覆盖颜色。
        /// </summary>
        [HarmonyPatch(typeof(Thing), "get_DrawColor")]
        [HarmonyPostfix]
        public static void DrawColorPostfix(Thing __instance, ref Color __result)
        {
            if (__instance.def == null) return;

            // 检查是否需要禁用染色
            if (ShouldDisableStuffColor(__instance.def))
            {
                // 强制颜色为纯白。在RimWorld的渲染管线中，这意味着渲染器将直接使用贴图文件本身的像素颜色，而不进行任何颜色混合。
                __result = Color.white;
            }
        }

        /// <summary>
        /// 检查并缓存一个ThingDef是否需要禁用材料染色。
        /// </summary>
        private static bool ShouldDisableStuffColor(ThingDef def)
        {
            // 如果缓存中有结果，直接返回，避免重复计算。
            if (cachedResults.TryGetValue(def, out bool result)) return result;

            // 查找XML中的ModExtension。
            var ext = def.GetModExtension<RavenStuffColorDisableExtension>();
            // 如果有扩展或DefName是渡鸦墙体，则禁用染色。
            bool disable = (ext != null && ext.disableStuffColor) || (def.defName == "Raven_Wall");

            // 存入缓存并返回结果。
            cachedResults[def] = disable;
            return disable;
        }

        /// <summary>
        /// 当Mod设置更改时，清空缓存，以便重新评估。
        /// </summary>
        public static void ClearCache() => cachedResults.Clear();

        /// <summary>
        /// 补丁目标：GenSpawn.SpawningWipes 方法。
        /// 在原版方法执行前，检查新旧建筑是否都是“墙体”类型。
        /// </summary>
        [HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
        [HarmonyPrefix]
        public static bool SpawningWipesPrefix(ref bool __result, BuildableDef newEntDef, BuildableDef oldEntDef)
        {
            ThingDef newDef = newEntDef as ThingDef;
            ThingDef oldDef = oldEntDef as ThingDef;

            if (newDef == null || oldDef == null) return true;

            // 获取蓝图对应的最终建筑Def
            ThingDef newBuiltDef = GenConstruct.BuiltDefOf(newDef) as ThingDef;
            ThingDef oldBuiltDef = GenConstruct.BuiltDefOf(oldDef) as ThingDef;

            if (newBuiltDef == null || oldBuiltDef == null) return true;

            // 判断新旧建筑是否都是“墙” (原版墙或我们的渡鸦墙)
            bool newIsWall = newBuiltDef == ThingDefOf.Wall || newBuiltDef.thingClass == typeof(RavenWall_Building);
            bool oldIsWall = oldBuiltDef == ThingDefOf.Wall || oldBuiltDef.thingClass == typeof(RavenWall_Building);

            if (newIsWall && oldIsWall)
            {
                // 如果都是墙，则强制允许覆盖建造
                __result = true;
                // 阻止原版方法执行
                return false;
            }

            // 如果不是墙体之间的替换，则继续执行原版逻辑
            return true;
        }
    }
}