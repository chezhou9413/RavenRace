using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MechanicalAngel
{
    public class JobDriver_AegisLustCharge : JobDriver
    {
        private TargetIndex MasterInd = TargetIndex.A;
        private const int ChargeDuration = 2500;
        private const int TicksBetweenMotes = 100;

        private Pawn Master => (Pawn)job.GetTarget(MasterInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Master, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(MasterInd);
            this.FailOn(() => Master.Dead);

            yield return Toils_Goto.GotoThing(MasterInd, PathEndMode.Touch);

            Toil prepare = ToilMaker.MakeToil("PrepareCharge");
            prepare.initAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Master);
                if (!Master.Downed && Master.health.capacities.CanBeAwake)
                {
                    Master.pather.StopDead();
                    Job layJob = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
                    layJob.expiryInterval = ChargeDuration + 100;
                    Master.jobs.StartJob(layJob, JobCondition.InterruptForced);
                    Master.rotationTracker.FaceCell(pawn.Position);
                }
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            };
            prepare.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return prepare;

            Toil chargeToil = ToilMaker.MakeToil("AegisCharging");
            chargeToil.defaultCompleteMode = ToilCompleteMode.Delay;
            chargeToil.defaultDuration = ChargeDuration;
            chargeToil.socialMode = RandomSocialMode.Off;
            chargeToil.WithProgressBarToilDelay(MasterInd);

            chargeToil.FailOn(() => !Master.Downed && Master.CurJobDef != JobDefOf.Wait_MaintainPosture);

            chargeToil.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Master);

                if (pawn.needs != null && pawn.needs.energy != null)
                {
                    float energyToRestorePerTick = pawn.needs.energy.MaxLevel / ChargeDuration;
                    pawn.needs.energy.CurLevel += energyToRestorePerTick;
                }

                if (Master.needs?.rest != null)
                {
                    Master.needs.rest.CurLevel -= Need_Rest.BaseRestGainPerTick * 3f;
                }

                if (pawn.IsHashIntervalTick(TicksBetweenMotes))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart, 0.4f);
                }
            };

            // 【核心新增】利用与上面刚写的一致的底层拦截机制，直接播放持续音效！
            SoundDef papapaSound = DefDatabase<SoundDef>.GetNamedSilentFail("RavenMechAegis_PaPaPa");
            if (papapaSound != null)
            {
                chargeToil.PlaySustainerOrSound(() => papapaSound, 1f);
            }

            yield return chargeToil;

            Toil successToil = ToilMaker.MakeToil("AegisChargeSuccess");
            successToil.initAction = delegate
            {
                if (Master != null && !Master.Dead && !Master.Destroyed)
                {
                    if (RavenDefOf.Raven_Hediff_AegisDrained != null)
                        Master.health.AddHediff(RavenDefOf.Raven_Hediff_AegisDrained);

                    if (RavenDefOf.Raven_Thought_AegisDrained != null && Master.needs?.mood != null)
                        Master.needs.mood.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_AegisDrained, pawn);

                    // 【核心修复】增加交配次数
                    Master.records?.Increment(RavenDefOf.Raven_Record_LovinCount);
                    pawn.records?.Increment(RavenDefOf.Raven_Record_LovinCount);

                    Messages.Message($"艾吉斯完成了对 {Master.LabelShort} 的体液榨取，淫能已满。", pawn, MessageTypeDefOf.NeutralEvent);
                }
            };
            successToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return successToil;

            this.AddFinishAction(delegate
            {
                if (Master != null && !Master.Dead && Master.CurJobDef == JobDefOf.Wait_MaintainPosture)
                {
                    Master.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            });
        }
    }
}