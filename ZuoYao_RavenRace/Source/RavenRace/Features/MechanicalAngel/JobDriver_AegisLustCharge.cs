using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MechanicalAngel
{
    /// <summary>
    /// 机械天使强制榨取体液补充淫能的过程。
    /// </summary>
    public class JobDriver_AegisLustCharge : JobDriver
    {
        private TargetIndex MasterInd = TargetIndex.A;
        private const int ChargeDuration = 2500; // 榨汁持续时间
        private const int TicksBetweenMotes = 100;

        private Pawn Master => (Pawn)job.GetTarget(MasterInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 必须预定主人
            return pawn.Reserve(Master, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(MasterInd);
            this.FailOn(() => Master.Dead);

            // 1. 飞向/走到主人身边
            yield return Toils_Goto.GotoThing(MasterInd, PathEndMode.Touch);

            // 2. 准备阶段：强迫主人躺下
            Toil prepare = ToilMaker.MakeToil("PrepareCharge");
            prepare.initAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Master);
                if (!Master.Downed && Master.health.capacities.CanBeAwake)
                {
                    Master.pather.StopDead();
                    Job layJob = JobMaker.MakeJob(JobDefOf.LayDown, pawn);
                    Master.jobs.StartJob(layJob, JobCondition.InterruptForced);
                    Master.rotationTracker.FaceCell(pawn.Position);
                }

                // 标记渲染树为脏，以触发粉色爱心眼的切换
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            };
            prepare.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return prepare;

            // 3. 持续榨取（充能）阶段
            Toil chargeToil = ToilMaker.MakeToil("AegisCharging");
            chargeToil.defaultCompleteMode = ToilCompleteMode.Delay;
            chargeToil.defaultDuration = ChargeDuration;
            chargeToil.socialMode = RandomSocialMode.Off;
            chargeToil.WithProgressBarToilDelay(MasterInd);

            chargeToil.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Master);

                // 给机械天使恢复淫能 (MechEnergy)
                if (pawn.needs?.energy != null)
                {
                    // 根据持续时间将能量回满。MaxLevel 通常是 100。
                    float energyToRestorePerTick = pawn.needs.energy.MaxLevel / ChargeDuration;
                    pawn.needs.energy.CurLevel += energyToRestorePerTick;
                }

                // 榨取主人的精力 (Rest)
                if (Master.needs?.rest != null)
                {
                    Master.needs.rest.CurLevel -= Need_Rest.BaseRestGainPerTick * 2f;
                }

                // 视觉效果：爱心
                if (pawn.IsHashIntervalTick(TicksBetweenMotes))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart, 0.4f);
                }
            };

            // 4. 结束处理
            chargeToil.AddFinishAction(delegate
            {
                if (Master != null && !Master.Dead)
                {
                    // 给主人添加“被榨干”的生理状态
                    HediffDef drainedDef = DefDatabase<HediffDef>.GetNamed("Raven_Hediff_AegisDrained");
                    if (drainedDef != null)
                    {
                        Master.health.AddHediff(drainedDef);
                    }

                    // 给主人添加记忆（巨额心情）
                    ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamed("Raven_Thought_AegisDrained");
                    if (thoughtDef != null && Master.needs?.mood != null)
                    {
                        Master.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, pawn);
                    }

                    // 结束主人的被迫躺下状态
                    if (Master.CurJobDef == JobDefOf.LayDown)
                    {
                        Master.jobs.EndCurrentJob(JobCondition.Succeeded);
                    }

                    Messages.Message($"艾吉斯完成了对 {Master.LabelShort} 的体液榨取，淫能已满。", pawn, MessageTypeDefOf.NeutralEvent);
                }

                // 标记渲染树为脏，恢复正常的眼睛贴图
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            });

            yield return chargeToil;
        }
    }
}