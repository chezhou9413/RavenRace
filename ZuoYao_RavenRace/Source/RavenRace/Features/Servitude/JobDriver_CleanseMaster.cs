using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.Servitude
{
    public class JobDriver_CleanseMaster : JobDriver
    {
        private const int DurationTicks = 400;
        private Pawn Master => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Master, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            // 如果主人离开床或浴缸，任务失败
            this.FailOn(() => !Master.InBed() && (Master.CurJobDef != RavenDefOf.Raven_Job_TakeSlimeBath || Master.GetPosture() != PawnPosture.LayingOnGroundFaceUp));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil wait = Toils_General.Wait(DurationTicks, TargetIndex.None);
            wait.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            // [错误修复] 使用存在的音效定义
            wait.PlaySustainerOrSound(SoundDefOf.Interact_CleanFilth);
            wait.tickAction = delegate ()
            {
                pawn.rotationTracker.FaceTarget(Master);
            };
            yield return wait;

            Toil finish = ToilMaker.MakeToil("Finish");
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            finish.initAction = delegate
            {
                // [错误修复] 使用存在的 ThoughtDef
                if (Master.needs?.mood != null)
                {
                    Master.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.RescuedMe, pawn);
                }
            };
            yield return finish;
        }
    }
}