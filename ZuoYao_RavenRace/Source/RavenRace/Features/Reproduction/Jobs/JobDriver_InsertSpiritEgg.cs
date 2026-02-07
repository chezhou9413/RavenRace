using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using RavenRace.Features.Reproduction; // [Added]

namespace RavenRace
{
    public class JobDriver_InsertSpiritEgg : JobDriver
    {
        private const TargetIndex PawnInd = TargetIndex.A;
        private const TargetIndex EggInd = TargetIndex.B;

        protected Pawn TargetPawn => (Pawn)job.GetTarget(PawnInd).Thing;
        protected Thing Egg => job.GetTarget(EggInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetPawn, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(Egg, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(PawnInd);
            this.FailOnDestroyedOrNull(EggInd);

            yield return Toils_Goto.GotoThing(EggInd, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(EggInd);
            yield return Toils_Haul.StartCarryThing(EggInd, false, true, false);

            yield return Toils_Goto.GotoThing(PawnInd, PathEndMode.Touch);

            Toil prepare = Toils_General.Wait(120);
            prepare.WithProgressBarToilDelay(PawnInd);
            prepare.FailOnCannotTouch(PawnInd, PathEndMode.Touch);
            yield return prepare;

            Toil insert = ToilMaker.MakeToil("InsertEgg");
            insert.initAction = delegate
            {
                Thing carriedEgg = pawn.carryTracker.CarriedThing;
                if (carriedEgg == null) return;

                Hediff existingHediff = TargetPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Raven_Hediff_SpiritEggInserted"));

                // [Change] HediffComp_SpiritEggHolder -> HediffCompSpiritEggHolder
                HediffCompSpiritEggHolder comp;

                if (existingHediff != null)
                {
                    comp = existingHediff.TryGetComp<HediffCompSpiritEggHolder>();
                }
                else
                {
                    Hediff newHediff = TargetPawn.health.AddHediff(HediffDef.Named("Raven_Hediff_SpiritEggInserted"));
                    comp = newHediff.TryGetComp<HediffCompSpiritEggHolder>();
                }

                if (comp != null)
                {
                    int acceptedCount = comp.TryAcceptThing(carriedEgg);

                    if (acceptedCount > 0)
                    {
                        pawn.carryTracker.innerContainer.Take(carriedEgg, acceptedCount);

                        string msg = TargetPawn.gender == Gender.Female ? "RavenRace_Message_EggInsertedFemale".Translate(TargetPawn.LabelShort) : "RavenRace_Message_EggInsertedMale".Translate(TargetPawn.LabelShort);
                        Messages.Message(msg + $" ({acceptedCount}个)", TargetPawn, MessageTypeDefOf.NeutralEvent, false);
                    }
                    else
                    {
                        Messages.Message("RavenRace_Fail_Full".Translate(TargetPawn.LabelShort), TargetPawn, MessageTypeDefOf.RejectInput, false);
                    }
                }
            };
            insert.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return insert;
        }
    }
}