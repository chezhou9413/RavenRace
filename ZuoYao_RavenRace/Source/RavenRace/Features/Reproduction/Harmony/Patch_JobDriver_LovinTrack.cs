using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.Reproduction.Harmony
{
    /// <summary>
    /// 全局动作追踪补丁：无需硬编码特定 Job，通过 XML 的 DefModExtension 实现泛用化拦截。
    /// </summary>
    [HarmonyPatch(typeof(JobDriver), "Cleanup")]
    public static class Patch_JobDriver_LovinTrack
    {
        [HarmonyPrefix]
        public static void Prefix(JobDriver __instance, JobCondition condition)
        {
            // 只有成功完成的工作才会处理
            if (condition == JobCondition.Succeeded && __instance.job != null && __instance.job.def != null)
            {
                // 检查该 Job 是否在 XML 中被打上了“交配动作”的标签
                if (__instance.job.def.HasModExtension<DefModExtension_LovinJob>())
                {
                    // 调用带防抖机制的安全增加方法
                    RavenReproductionUtility.AddLovinCountSafely(__instance.pawn);
                }
            }
        }
    }
}