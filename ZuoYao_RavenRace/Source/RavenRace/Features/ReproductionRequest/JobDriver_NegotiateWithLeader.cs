using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.AI.Group;

namespace RavenRace.Features.ReproductionRequest
{
    /// <summary>
    /// “与领队交涉”的工作驱动。
    /// 包含两个步骤：1. 走到目标身边。 2. 到达后打开对话框。
    /// </summary>
    public class JobDriver_NegotiateWithLeader : JobDriver
    {
        private Pawn Leader => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Leader, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !(Leader.GetLord()?.LordJob is LordJob_ReproductionRequest lordJob) || !lordJob.isWaitingForDialog);

            // 1. 走到领队身边
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // 2. 到达后，打开对话框
            Toil openDialog = ToilMaker.MakeToil("OpenDialog");
            openDialog.initAction = () =>
            {
                if (Leader.GetLord()?.LordJob is LordJob_ReproductionRequest lordJob)
                {
                    // [修复] 核心修复点：调用正确的类名 FloatMenuOptionProvider_ReproductionRequest
                    FloatMenuOptionProvider_ReproductionRequest.OpenReproductionDialog(pawn, lordJob);
                }
            };
            openDialog.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return openDialog;
        }
    }
}