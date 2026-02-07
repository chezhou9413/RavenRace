using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using RavenRace.Features.Reproduction; // [Added]

namespace RavenRace
{
    public class JobDriver_RemoveSpiritEgg : JobDriver
    {
        private const TargetIndex PawnInd = TargetIndex.A;
        protected Pawn TargetPawn => (Pawn)job.GetTarget(PawnInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetPawn, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(PawnInd);

            yield return Toils_Goto.GotoThing(PawnInd, PathEndMode.Touch);

            Toil prepare = Toils_General.Wait(120);
            prepare.WithProgressBarToilDelay(PawnInd);
            prepare.FailOnCannotTouch(PawnInd, PathEndMode.Touch);
            yield return prepare;

            Toil remove = ToilMaker.MakeToil("RemoveEgg");
            remove.initAction = delegate
            {
                Hediff hediff = TargetPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Raven_Hediff_SpiritEggInserted"));
                if (hediff != null)
                {
                    // [Change] HediffComp_SpiritEggHolder -> HediffCompSpiritEggHolder
                    var comp = hediff.TryGetComp<HediffCompSpiritEggHolder>();
                    if (comp != null)
                    {
                        comp.EjectEgg(false);
                    }
                }
            };
            remove.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return remove;
        }
    }
}