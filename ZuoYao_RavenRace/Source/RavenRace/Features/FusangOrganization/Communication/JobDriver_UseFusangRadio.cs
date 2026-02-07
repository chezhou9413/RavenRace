using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RavenRace
{
    public class JobDriver_UseFusangRadio : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 预定电台，防止多人同时操作
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);

            // 1. 走到电台
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 2. 打开界面
            Toil openUi = new Toil();
            openUi.initAction = () =>
            {
                Thing radio = job.targetA.Thing;
                if (radio != null && !radio.Destroyed)
                {
                    Find.WindowStack.Add(new Dialog_FusangComm(radio));
                }
            };
            openUi.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return openUi;
        }
    }
}