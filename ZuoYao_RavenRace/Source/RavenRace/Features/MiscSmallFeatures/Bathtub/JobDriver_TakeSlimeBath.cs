using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.Bathtub
{
    public class JobDriver_TakeSlimeBath : JobDriver
    {
        private Building_RavenBathtub Bathtub => job.GetTarget(TargetIndex.A).Thing as Building_RavenBathtub;
        private IntVec3 BathCell => job.GetTarget(TargetIndex.B).Cell;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Bathtub, job, job.def.joyMaxParticipants, 0, null, errorOnFailed) &&
                   pawn.Reserve(BathCell, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);

            // 1. 寻路走到浴缸内部的具体格子上
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            // 2. 走到位后，开始泡澡逻辑
            Toil takeBath = ToilMaker.MakeToil("TakeSlimeBath");
            takeBath.defaultCompleteMode = ToilCompleteMode.Delay;
            takeBath.defaultDuration = job.def.joyDuration;
            takeBath.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            takeBath.socialMode = RandomSocialMode.SuperActive;

            takeBath.initAction = delegate
            {
                // 只有走到位了，才把姿态改为躺下
                pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                pawn.pather.StopDead();

                // 姿态改变后，强制刷新 1.6 的贴图缓存。
                // 此时 Harmony 补丁检测到姿态正确，瞬间脱衣并偏移坐标！
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            };

            takeBath.tickAction = delegate
            {
                pawn.pather.StopDead();
                pawn.GainComfortFromCellIfPossible(1);

                if (pawn.needs != null && pawn.needs.joy != null)
                {
                    float joyGainFactor = Bathtub.GetStatValue(StatDefOf.JoyGainFactor, true, -1);
                    float joyAmount = (joyGainFactor * job.def.joyGainRate * 0.36f) / 2500f;

                    pawn.needs.joy.GainJoy(joyAmount, job.def.joyKind);

                    if (RavenRaceMod.Settings.bathtubDisableTolerance)
                    {
                        if (pawn.needs.joy.CurLevel > 0.9999f && !job.doUntilGatheringEnded)
                        {
                            ReadyForNextToil();
                        }
                    }
                    else
                    {
                        JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.EndJob, 1f, Bathtub);
                    }
                }

                if (pawn.IsHashIntervalTick(100) && pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart, 0.42f);
                }
            };

            takeBath.AddFinishAction(delegate
            {
                if (pawn == null || pawn.Destroyed) return;

                if (RavenDefOf.Raven_Thought_SlimeBath != null)
                {
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RavenDefOf.Raven_Thought_SlimeBath);
                }

                // 泡澡结束（无论是正常结束还是被强行打断）。
                // 原版底层的 Job 结束机制会自动恢复 Standing 姿态。
                // 我们只需强制刷新贴图缓存，Harmony 补丁判定失败，瞬间穿回衣服并恢复坐标！
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            });

            yield return takeBath;
        }
    }
}