using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.MasturbatorCup
{
    /// <summary>
    /// 补丁：强制显示背包中“飞机杯”的操作按钮。
    /// 原版 Pawn.GetGizmos 不会深度遍历 Inventory 中的所有 Comp 并调用 CompGetGizmosExtra。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_InventoryGizmos
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            // 1. 返回原有的 Gizmos
            foreach (var g in values)
            {
                yield return g;
            }

            // 2. 仅针对玩家控制的殖民者
            if (__instance == null || !__instance.IsColonistPlayerControlled || __instance.inventory == null) yield break;

            // 3. 遍历背包 (Inventory)
            // 注意：innerContainer 是 ThingOwner，可以直接 foreach
            var container = __instance.inventory.innerContainer;
            for (int i = 0; i < container.Count; i++)
            {
                Thing item = container[i];

                // 检查是否为飞机杯
                if (item.def == RavenDefOf.Raven_Item_MasturbatorCup)
                {
                    var comp = item.TryGetComp<CompMasturbatorCup>();
                    if (comp != null)
                    {
                        foreach (var extraGizmo in comp.GetInventoryGizmos(__instance))
                        {
                            yield return extraGizmo;
                        }
                    }
                }
            }
        }
    }
}