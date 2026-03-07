using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RavenRace.Features.DickRain
{
    public class JobDriver_YuwangNanRen : JobDriver
    {
        //动画
        private UnityEngine.Vector3 jitterOffset = UnityEngine.Vector3.zero;
        public override UnityEngine.Vector3 ForcedBodyOffset => jitterOffset;

        //常量
        private const int DurationTicks = 900;
        private const int HeartMoteInterval = 80;
        private const int FilthSplashInterval = 5;
        private const int FilthDropInterval = 30;
        private const float FilthDropChance = 0.4f;
        private const float BloodLossAmount = 0.25f;

        //属性
        protected Pawn TargetPawn => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => TargetPawn.Dead);
            Toil gotoBack = ToilMaker.MakeToil("GotoBack");
            gotoBack.initAction = delegate
            {
                IntVec3 backCell = TargetPawn.Position - TargetPawn.Rotation.FacingCell;
                if (!backCell.Walkable(pawn.Map))
                    backCell = TargetPawn.Position;
                pawn.pather.StartPath(backCell, PathEndMode.OnCell);
            };
            gotoBack.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            gotoBack.FailOnDespawnedOrNull(TargetIndex.A);
            yield return gotoBack;
            Toil actToil = ToilMaker.MakeToil("Act");
            actToil.initAction = delegate
            {
                TargetPawn.jobs?.StopAll();
                TargetPawn.needs?.mood?.thoughts?.memories?
                    .TryGainMemory(ThoughtDefOf.HarmedMe, pawn);
            };

            actToil.tickAction = delegate
            {
                IntVec3 backCell = TargetPawn.Position - TargetPawn.Rotation.FacingCell;
                if (pawn.Position != backCell && backCell.Walkable(pawn.Map))
                    pawn.Position = backCell;
                pawn.Rotation = TargetPawn.Rotation;
                TargetPawn.pather.StopDead();
                float mag = (float)Math.Sin(Find.TickManager.TicksGame * 0.6f) * 0.12f;
                jitterOffset = TargetPawn.Rotation.IsHorizontal
                    ? new UnityEngine.Vector3(mag, 0f, 0f)
                    : new UnityEngine.Vector3(0f, 0f, mag);
                if (pawn.IsHashIntervalTick(HeartMoteInterval))
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                if (pawn.IsHashIntervalTick(FilthSplashInterval))
                    FilthMaker.TryMakeFilth(pawn.Position, pawn.Map,
                        ThingDefOf.Filth_Slime, pawn.LabelIndefinite(), 1);
                if (pawn.IsHashIntervalTick(FilthDropInterval) && Rand.Chance(FilthDropChance))
                    FilthMaker.TryMakeFilth(TargetPawn.Position, pawn.Map,
                        TargetPawn.RaceProps.BloodDef, TargetPawn.LabelIndefinite(), 1);
            };
            actToil.defaultCompleteMode = ToilCompleteMode.Delay;
            actToil.defaultDuration = DurationTicks;
            actToil.socialMode = RandomSocialMode.Off;
            actToil.WithProgressBar(TargetIndex.A,
                () => 1f - (float)ticksLeftThisToil / DurationTicks);
            actToil.AddFinishAction(delegate
            {
                jitterOffset = UnityEngine.Vector3.zero;

                if (TargetPawn == null || TargetPawn.Dead) return;
                HealthUtility.AdjustSeverity(TargetPawn, HediffDefOf.BloodLoss, BloodLossAmount);

                //施害方：获得心情记忆
                Thought_Memory lovinMemory =
                    ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin) as Thought_Memory;
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(lovinMemory, TargetPawn);

                //通知精神状态任务完成
                MentalState_DickRainLust lustState =
                    pawn.MentalState as MentalState_DickRainLust;
                if (lustState != null)
                    lustState.Notify_ActCompleted();
            });
            yield return actToil;
        }
    }
}