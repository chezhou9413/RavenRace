using System.Collections.Generic;
using RavenRace.Features.Reproduction;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RavenRace.Features.ReproductionRequest
{
    /// <summary>
    /// 短时间排队交配逻辑。只加次数、加心情、强制目标硬直。不怀孕。
    /// </summary>
    public class JobDriver_RequestLovin : JobDriver
    {
        private const int LovinDuration = 400; // 较短时间即可完事

        protected Pawn TargetMale => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetMale, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => TargetMale.Dead || TargetMale.Downed);

            // 1. 走到目标身边
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // 2. 开始榨取
            Toil lovinToil = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = LovinDuration,
                socialMode = RandomSocialMode.Off
            };

            lovinToil.tickAction = delegate
            {
                pawn.rotationTracker.FaceCell(TargetMale.Position);
                TargetMale.rotationTracker.FaceCell(pawn.Position);

                // 冒爱心
                if (pawn.IsHashIntervalTick(50))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart, 0.42f);
                    FleckMaker.ThrowMetaIcon(TargetMale.Position, TargetMale.Map, FleckDefOf.Heart, 0.42f);
                }

                // 【核心防逃跑】：持续给男性派发原地等待的任务
                if (TargetMale.CurJobDef != JobDefOf.Wait_MaintainPosture)
                {
                    Job waitJob = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
                    TargetMale.jobs.StartJob(waitJob, JobCondition.InterruptForced);
                }
            };

            lovinToil.AddFinishAction(delegate
            {
                if (TargetMale != null && !TargetMale.Dead)
                {
                    // 添加双方的色色心情
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ReproductionRequestDefOf.Raven_Thought_GroupLovinParticipant);

                    // 防抖式增加交配次数（核心联动）
                    RavenReproductionUtility.AddLovinCountSafely(TargetMale);

                    // 释放男性硬直
                    if (TargetMale.CurJobDef == JobDefOf.Wait_MaintainPosture)
                    {
                        TargetMale.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                }

                // 通知主状态机：本姑娘做完了，换下一个
                if (pawn.GetLord()?.LordJob is LordJob_ReproductionRequest lordJob)
                {
                    lordJob.Notify_FemaleFinishedLovin(pawn);
                }
            });

            yield return lovinToil;
        }
    }
}