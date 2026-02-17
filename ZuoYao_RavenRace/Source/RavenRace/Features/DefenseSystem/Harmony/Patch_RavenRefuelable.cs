using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using RavenRace.Features.DefenseSystem.Traps;

namespace RavenRace.Features.DefenseSystem.Harmony
{
    /// <summary>
    /// 拦截 CompRefuelable 的 Refuel(List<Thing>) 方法。
    /// 仅针对 CompRavenOrganicRefuelable 生效，将“物品数量装填”改为“营养值装填”。
    /// </summary>
    [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.Refuel), new System.Type[] { typeof(List<Thing>) })]
    public static class Patch_RavenRefuelable
    {
        [HarmonyPrefix]
        public static bool Prefix(CompRefuelable __instance, List<Thing> fuelThings)
        {
            // 1. 检查是否为我们的自定义组件
            if (!(__instance is CompRavenOrganicRefuelable))
            {
                return true; // 不是我们的陷阱，执行原版逻辑
            }

            float totalNutrition = 0f;

            // 2. 计算总营养值并销毁物品
            for (int i = fuelThings.Count - 1; i >= 0; i--)
            {
                Thing t = fuelThings[i];
                float nutritionPerItem = t.GetStatValue(StatDefOf.Nutrition);
                totalNutrition += nutritionPerItem * t.stackCount;

                t.Destroy(DestroyMode.Vanish);
                fuelThings.RemoveAt(i);
            }

            // 3. 计算获得的燃料次数
            // 逻辑：四舍五入。例如 0.8营养 -> 1次， 0.2营养 -> 0次
            float charges = Mathf.Round(totalNutrition);

            // 保底逻辑：只要有营养输入，至少给1次，防止玩家放入小肉块后什么都没发生
            if (charges < 1f && totalNutrition > 0.01f) charges = 1f;

            if (charges > 0)
            {
                __instance.Refuel(charges);
            }

            // 返回 false 阻止原版方法执行（原版是按 stackCount 加油）
            return false;
        }
    }
}