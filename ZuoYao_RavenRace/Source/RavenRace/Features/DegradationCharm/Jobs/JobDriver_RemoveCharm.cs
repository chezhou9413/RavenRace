using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RavenRace.Features.DegradationCharm.Jobs
{
    public class JobDriver_RemoveCharm : JobDriver
    {
        private const int Duration = 120;
        private Pawn TargetPawn => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetPawn, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil waitToil = Toils_General.Wait(Duration, TargetIndex.A);
            waitToil.WithProgressBarToilDelay(TargetIndex.A);
            waitToil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return waitToil;

            Toil removeCharm = ToilMaker.MakeToil("RemoveCharm");
            removeCharm.initAction = delegate
            {
                if (TargetPawn != null && !TargetPawn.Dead)
                {
                    Hediff hediff = TargetPawn.health.hediffSet.GetFirstHediffOfDef(DegradationCharmDefOf.Raven_Hediff_Degradation);
                    if (hediff != null)
                    {
                        TargetPawn.health.RemoveHediff(hediff);
                        GenPlace.TryPlaceThing(ThingMaker.MakeThing(DegradationCharmDefOf.Raven_Item_CorruptionTalisman), TargetPawn.Position, TargetPawn.Map, ThingPlaceMode.Near);
                        Messages.Message("Raven_Message_CharmRemoved".Translate(pawn.LabelShort, TargetPawn.LabelShort), TargetPawn, MessageTypeDefOf.NeutralEvent);
                        SoundDefOf.FlickSwitch.PlayOneShot(new TargetInfo(TargetPawn.Position, TargetPawn.Map));
                    }
                }
            };
            removeCharm.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return removeCharm;
        }
    }
}