using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RavenRace.Features.MiscSmallFeatures.AVRecording; // [新增] 引入AV记录命名空间

namespace RavenRace.Features.Reproduction.Harmony
{
    /// <summary>
    /// 全局动作追踪补丁：无需硬编码特定 Job，通过 XML 的 DefModExtension 实现泛用化拦截。
    /// 职责：
    /// 1. 记录做爱次数
    /// 2. [新增] 触发 AV摄影机 系统的事件总线
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
                    // 1. 调用带防抖机制的安全增加交配次数方法 (原有逻辑，不动)
                    RavenReproductionUtility.AddLovinCountSafely(__instance.pawn);

                    // 2. [新增] 触发 AV 摄影机广播事件
                    // 在原版的 Lovin 和我们的 ForceLovin 中，伴侣都被存储在 TargetA 中。
                    Pawn partner = __instance.job.GetTarget(TargetIndex.A).Thing as Pawn;

                    // 防抖处理：双人床做爱时，双方的小人都会触发各自的 Lovin 结束。
                    // 为了防止瞬间产出两盘带子，我们规定：只让 ID 较小的一方负责发送摄影通知。
                    bool isInitiator = true;
                    if (partner != null)
                    {
                        isInitiator = __instance.pawn.thingIDNumber < partner.thingIDNumber;
                    }

                    if (isInitiator && __instance.pawn.Map != null)
                    {
                        var avManager = __instance.pawn.Map.GetComponent<MapComponent_AVManager>();
                        if (avManager != null)
                        {
                            avManager.Notify_LovinFinished(__instance.pawn, partner);
                        }
                    }
                }
            }
        }
    }
}