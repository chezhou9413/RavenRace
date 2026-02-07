using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.Reproduction
{
    public class ThingSpiritEgg : ThingWithComps
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var opt in base.GetFloatMenuOptions(selPawn)) yield return opt;

            foreach (Pawn target in this.Map.mapPawns.AllPawnsSpawned)
            {
                if (target.RaceProps.Humanlike && target.Faction == Faction.OfPlayer)
                {
                    Hediff hediff = target.health.hediffSet.GetFirstHediffOfDef(RavenDefOf.Raven_Hediff_SpiritEggInserted);
                    bool isFull = false;

                    if (hediff != null)
                    {
                        var comp = hediff.TryGetComp<HediffCompSpiritEggHolder>();
                        if (comp != null && comp.innerContainer != null && comp.innerContainer.Count >= 5)
                        {
                            isFull = true;
                        }
                    }

                    if (isFull)
                    {
                        string labelFull = "RavenRace_FloatMenu_InsertEgg".Translate(target.LabelShort);
                        yield return new FloatMenuOption(labelFull + " (" + "RavenRace_Fail_Full".Translate(target.LabelShort) + ")", null);
                    }
                    else
                    {
                        string label = "RavenRace_FloatMenu_InsertEgg".Translate(target.LabelShort);
                        yield return new FloatMenuOption(label, () =>
                        {
                            Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_InsertSpiritEgg, target, this);
                            job.count = 1;
                            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        });
                    }
                }
            }
        }
    }
}