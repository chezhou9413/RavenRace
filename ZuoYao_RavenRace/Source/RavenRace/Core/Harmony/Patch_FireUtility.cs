using HarmonyLib;
using RimWorld;
using Verse;
using RavenRace.Features.Bloodline;

namespace RavenRace.Core.Harmony
{
    /// <summary>
    /// 全局防火补丁，用于实现“机械体血脉”的防火特性。
    /// 此补丁拦截游戏内所有对“是否可燃”的判定，如果目标是拥有机械血脉的Pawn，则强制其不可燃。
    /// </summary>
    [HarmonyPatch(typeof(FireUtility), "CanEverAttachFire")]
    public static class Patch_FireUtility_CanEverAttachFire
    {
        /// <summary>
        /// 在原版CanEverAttachFire方法执行前运行的前缀补丁。
        /// </summary>
        /// <param name="t">被检查的Thing。</param>
        /// <param name="__result">用于存储最终结果的引用。我们可以修改它并跳过原版方法。</param>
        /// <returns>返回true则继续执行原版方法，返回false则跳过。</returns>
        [HarmonyPrefix]
        public static bool Prefix(Thing t, ref bool __result)
        {
            // 检查目标是否为Pawn
            if (t is Pawn p)
            {
                // 获取血脉组件
                var comp = p.TryGetComp<CompBloodline>();

                // 检查是否拥有机械体血脉
                // 这里的 Key "Bloodline_Mechanoid" 对应我们在 BloodlineManager 中定义的逻辑
                if (comp != null && comp.BloodlineComposition.ContainsKey(BloodlineManager.MECHANIOD_BLOODLINE_KEY))
                {
                    float value = comp.BloodlineComposition[BloodlineManager.MECHANIOD_BLOODLINE_KEY];
                    // 只要血脉浓度大于0，就赋予防火特性
                    if (value > 0f)
                    {
                        // 强制结果为“不可燃”
                        __result = false;
                        // 跳过原版方法，提高性能
                        return false;
                    }
                }
            }
            // 如果不是拥有机械血脉的Pawn，则执行原版的可燃性判断逻辑
            return true;
        }
    }
}