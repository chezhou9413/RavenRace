using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.Servitude
{
    /// <summary>
    /// 膝枕互动的具体执行驱动。
    /// 让主人躺在侍奉者腿上并获得休息值加成。
    /// </summary>
    public class JobDriver_LapPillow : JobDriver
    {
        private const int DurationTicks = 2500; // 膝枕持续时间
        private const TargetIndex MasterInd = TargetIndex.A;

        private Pawn Master => (Pawn)job.GetTarget(MasterInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 预定主人
            return pawn.Reserve(Master, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 失败条件：主人消失、倒地或醒着但由于某种原因无法继续
            this.FailOnDespawnedOrNull(MasterInd);
            this.FailOnDowned(MasterInd);
            this.FailOnNotAwake(MasterInd);

            // 1. 侍奉者走到主人旁边
            yield return Toils_Goto.GotoThing(MasterInd, PathEndMode.Touch);

            // 2. 设置姿态
            Toil setupToil = ToilMaker.MakeToil("SetupLapPillow");
            setupToil.initAction = delegate
            {
                // 让主人执行“躺下”任务，目标设为侍奉者
                Master.pather.StopDead();
                Job layJob = JobMaker.MakeJob(JobDefOf.LayDown, pawn);
                Master.jobs.StartJob(layJob, JobCondition.InterruptForced);
                Master.rotationTracker.FaceCell(pawn.Position);

                // 侍奉者停止移动并面对主人
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceTarget(Master);
            };
            setupToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return setupToil;

            // 3. 膝枕过程
            Toil pillowToil = ToilMaker.MakeToil("LapPillow");
            pillowToil.defaultCompleteMode = ToilCompleteMode.Delay;
            pillowToil.defaultDuration = DurationTicks;
            pillowToil.socialMode = RandomSocialMode.Off;
            pillowToil.WithProgressBarToilDelay(MasterInd);

            pillowToil.tickAction = delegate
            {
                // 维持朝向
                pawn.rotationTracker.FaceTarget(Master);
                Master.rotationTracker.FaceCell(pawn.Position);

                // [错误修复] 1.6 版本中 Need_Rest 没有 GainRest 方法。
                // 必须直接修改 CurLevel。我们使用原版的基准增量并给予 1.2 倍加成。
                if (Master.needs?.rest != null)
                {
                    // 引用反编译得到的 BaseRestGainPerTick 常量
                    float gainAmount = Need_Rest.BaseRestGainPerTick * 1.2f;
                    Master.needs.rest.CurLevel = Mathf.Min(Master.needs.rest.CurLevel + gainAmount, Master.needs.rest.MaxLevel);
                }

                // 视觉效果：周期性冒爱心
                if (pawn.IsHashIntervalTick(100))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                    FleckMaker.ThrowMetaIcon(Master.Position, Master.Map, FleckDefOf.Heart);
                }
            };

            // 结束时让主人停止躺下
            pillowToil.AddFinishAction(delegate {
                if (Master != null && !Master.Dead && Master.CurJobDef == JobDefOf.LayDown)
                {
                    Master.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            });

            yield return pillowToil;
        }
    }
}