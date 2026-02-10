using HarmonyLib;
using RimWorld;
using Verse;
using RavenRace.Features.Bloodline;

namespace RavenRace.Core.Harmony
{
    /// <summary>
    /// 防火逻辑补丁
    /// </summary>
    [HarmonyPatch(typeof(FireUtility), "CanEverAttachFire")]
    public static class Patch_FireUtility_CanEverAttachFire
    {
        /// <summary>
        /// 拦截是否可以附着火焰的判定。
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Thing t, ref bool __result)
        {
            if (t is Pawn p)
            {
                // 获取血脉组件
                var comp = p.TryGetComp<CompBloodline>();

                // 检查是否拥有机械体血脉
                // 这里的 Key "Bloodline_Mechanoid" 对应我们在 BloodlineManager 中定义的逻辑
                if (comp != null && comp.BloodlineComposition.ContainsKey("Bloodline_Mechanoid"))
                {
                    float value = comp.BloodlineComposition["Bloodline_Mechanoid"];
                    if (value > 0f)
                    {
                        // 强制不可燃
                        __result = false;
                        return false; // 跳过原版方法
                    }
                }
            }
            return true; // 执行原版方法
        }
    }
}