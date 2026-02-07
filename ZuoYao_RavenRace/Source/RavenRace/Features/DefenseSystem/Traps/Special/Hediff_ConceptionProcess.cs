using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Reproduction; // [Added]

namespace RavenRace
{
    public class Hediff_ConceptionProcess : HediffWithComps
    {
        private int ticksLeft = 2500;
        private Thing trapThing;
        private IntVec3 trapPos;

        public void Initialize(Thing trap)
        {
            this.trapThing = trap;
            this.trapPos = trap.Position;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
            Scribe_Values.Look(ref trapPos, "trapPos");
        }

        public override void Tick()
        {
            base.Tick();

            if (pawn.CurJobDef != JobDefOf.Wait_MaintainPosture)
            {
                pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture), Verse.AI.JobCondition.InterruptForced);
            }

            if (pawn.IsHashIntervalTick(100))
            {
                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart, 0.42f);
            }

            ticksLeft--;
            if (ticksLeft <= 0)
            {
                Finish();
            }
        }

        private void Finish()
        {
            pawn.health.RemoveHediff(this);

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Raven_Hediff_SpiritEggInserted"));
            if (hediff == null)
            {
                hediff = pawn.health.AddHediff(HediffDef.Named("Raven_Hediff_SpiritEggInserted"));
            }

            // [Change] HediffComp_SpiritEggHolder -> HediffCompSpiritEggHolder
            var comp = hediff.TryGetComp<HediffCompSpiritEggHolder>();
            if (comp != null)
            {
                Thing unfertilizedEggs = ThingMaker.MakeThing(ThingDef.Named("Raven_SpiritEgg_Unfertilized"));
                unfertilizedEggs.stackCount = 5;

                comp.TryAcceptThing(unfertilizedEggs);

                hediff.Severity = (float)comp.innerContainer.Count;
            }

            Messages.Message($"{pawn.LabelShort} 被强制灌注了大量灵卵！", pawn, MessageTypeDefOf.NegativeEvent);
        }
    }
}