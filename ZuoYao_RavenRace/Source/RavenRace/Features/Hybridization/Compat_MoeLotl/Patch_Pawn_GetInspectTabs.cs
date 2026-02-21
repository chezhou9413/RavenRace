using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using RavenRace.Compat.MoeLotl;

namespace RavenRace.Features.Hybridization.Harmony
{
    /// <summary>
    /// 动态为拥有萌螈血脉的渡鸦添加“修炼”标签页。
    /// 【最终修正版】
    /// 1. 补丁目标是声明虚方法的基类 Thing。
    /// 2. 方法内部安全地将 Thing 实例转换为 Pawn 进行逻辑判断。
    /// 3. 使用 [HarmonyPatch] 自动注册，无需在其他地方手动调用。
    /// </summary>
    [HarmonyPatch(typeof(Thing), "GetInspectTabs")]
    public static class Patch_Pawn_GetInspectTabs
    {
        [HarmonyPostfix]
        public static IEnumerable<InspectTabBase> Postfix(IEnumerable<InspectTabBase> __result, Thing __instance)
        {
            // 1. 首先，返回原版方法提供的所有标签页，这是必须的。
            foreach (var tab in __result)
            {
                yield return tab;
            }

            // 2. 检查实例是否是一个 Pawn，如果不是，则直接结束。
            if (__instance is Pawn pawn)
            {
                // 3. 对 Pawn 执行我们之前的逻辑
                // 确保只对我们的渡鸦族生效
                if (pawn.def.defName == "Raven_Race")
                {
                    // 检查设置、Mod激活状态和Pawn血脉
                    if (RavenRaceMod.Settings.enableMoeLotlCompat && MoeLotlCompatUtility.IsMoeLotlActive)
                    {
                        if (MoeLotlCompatUtility.HasMoeLotlBloodline(pawn))
                        {
                            // 从游戏缓存中获取共享的修炼ITab实例
                            // 这是正确且高效的方式
                            InspectTabBase moeLotlTab = InspectTabManager.GetSharedInstance(MoeLotlCompatUtility.ITabCultivationType);
                            if (moeLotlTab != null)
                            {
                                // 将它添加到返回的列表中，UI就会显示它
                                yield return moeLotlTab;
                            }
                        }
                    }
                }
            }
        }
    }
}