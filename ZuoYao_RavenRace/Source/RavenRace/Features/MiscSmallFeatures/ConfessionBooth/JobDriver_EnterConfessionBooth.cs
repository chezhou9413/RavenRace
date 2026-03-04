using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.ConfessionBooth
{
    /// <summary>
    /// 忏悔室进入 Job 的驱动器。
    /// 流程：Pawn 走到交互格 → 调用 TryAcceptPawn 进入容器 → Job 结束。
    /// 进入容器后，Pawn 的后续状态由建筑的 Tick 管理。
    /// </summary>
    public class JobDriver_EnterConfessionBooth : JobDriver
    {
        /// <summary>
        /// 预订目标建筑，允许最多 2 个 Pawn 同时预订同一忏悔室。
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // maxPawns: 2 允许修女和信徒同时预订同一建筑
            return pawn.Reserve(job.targetA, job, maxPawns: 2, stackCount: 0, null, errorOnFailed);
        }

        /// <summary>
        /// 定义 Job 的执行步骤：
        /// 1. 走到忏悔室的交互格
        /// 2. 进入容器（执行 TryAcceptPawn）
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 目标（忏悔室）被移除或摧毁时，Job 失败
            this.FailOnDespawnedOrNull(TargetIndex.A);

            Building_ConfessionBooth booth = job.targetA.Thing as Building_ConfessionBooth;

            // 如果目标不是忏悔室，或者容器已满（2人），立即失败
            // 注意：只在 Job 开始时检查一次，不在每帧检查，
            // 避免第一个人进入后（Count=1）第二个人因条件检查失败而中断
            this.FailOn(() => booth == null);

            // --- Toil 1：走到忏悔室的交互格 ---
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // --- Toil 2：进入容器 ---
            Toil enter = ToilMaker.MakeToil("EnterConfessionBooth");
            enter.initAction = delegate
            {
                // 检查建筑是否有效，以及容器是否还有空间
                if (booth != null && booth.Spawned && booth.CanAcceptPawn(pawn).Accepted)
                {
                    booth.TryAcceptPawn(pawn);
                }
                // 无论是否成功进入，Job 在此结束（Instant 模式）
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }
}