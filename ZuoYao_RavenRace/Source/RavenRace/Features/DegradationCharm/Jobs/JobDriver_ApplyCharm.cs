using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RavenRace.Features.DegradationCharm.Jobs
{
    /// <summary>
    /// 执行“贴上堕落符咒”工作的JobDriver。
    /// </summary>
    public class JobDriver_ApplyCharm : JobDriver
    {
        private const int Duration = 120; // 贴符动作持续时间 (2秒)

        private Pawn TargetPawn => (Pawn)job.targetA.Thing;
        private Thing Charm => job.targetB.Thing; // 符咒物品现在是 TargetB

        /// <summary>
        /// 在工作开始前预定目标Pawn。因为符咒已经在物品栏里，所以不需要预定。
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetPawn, job, 1, -1, null, errorOnFailed);
        }

        /// <summary>
        /// 定义工作流程。
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // --- 前置检查 ---
            this.FailOnDespawnedOrNull(TargetIndex.A); // 目标Pawn
            this.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch); // 必须能接触到目标
            // [新增] 检查物品栏里是否真的还有符咒，防止中途被丢掉
            this.FailOn(() => !pawn.inventory.innerContainer.Contains(Charm));

            // --- Toil 1: 移动到目标 ---
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // --- Toil 2: 等待并执行贴符动作 ---
            Toil applyToil = Toils_General.Wait(Duration, TargetIndex.A);
            applyToil.WithProgressBarToilDelay(TargetIndex.A);
            applyToil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            applyToil.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetPawn);
                if (pawn.IsHashIntervalTick(30))
                {
                    FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, FleckDefOf.Heart, 0.5f);
                }
            };
            yield return applyToil;

            // --- Toil 3: 完成贴符，消耗物品并添加Hediff ---
            Toil finishToil = ToilMaker.MakeToil("FinishApplyCharm");
            finishToil.initAction = delegate
            {
                if (TargetPawn != null && !TargetPawn.Dead && Charm != null && !Charm.Destroyed)
                {
                    // [核心修正] 确认物品还在物品栏里，然后消耗它
                    if (pawn.inventory.innerContainer.Contains(Charm))
                    {
                        // 消耗符咒
                        Charm.SplitOff(1).Destroy(DestroyMode.Vanish);

                        // 为目标添加“堕落刻印”状态
                        TargetPawn.health.AddHediff(DegradationCharmDefOf.Raven_Hediff_Degradation);
                        Messages.Message("Raven_Message_CharmApplied".Translate(pawn.LabelShort, TargetPawn.LabelShort), TargetPawn, MessageTypeDefOf.NeutralEvent);
                    }
                }
            };
            finishToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finishToil;
        }
    }
}