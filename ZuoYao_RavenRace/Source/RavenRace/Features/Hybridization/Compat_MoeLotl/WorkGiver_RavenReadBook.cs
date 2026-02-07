using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Compat.MoeLotl
{
    public class WorkGiver_RavenReadBook : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForUndefined();

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!MoeLotlCompatUtility.IsMoeLotlActive) return true;
            if (pawn.def.defName != "Raven_Race" || !MoeLotlCompatUtility.HasMoeLotlBloodline(pawn)) return true;

            return MoeLotlCompatUtility.GetTargetReadBook(pawn) == null;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            ThingDef targetDef = MoeLotlCompatUtility.GetTargetReadBook(pawn);
            if (targetDef == null) yield break;

            var books = pawn.Map.listerThings.ThingsOfDef(targetDef);
            foreach (var book in books)
            {
                if (!book.IsForbidden(pawn) && pawn.CanReach(book, PathEndMode.Touch, Danger.Deadly))
                {
                    yield return book;
                }
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.IsForbidden(pawn)) return false;

            ThingDef targetDef = MoeLotlCompatUtility.GetTargetReadBook(pawn);
            if (t.def != targetDef) return false;

            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("Axolotl_ReadMoeLotlQiSkillBooks");
            return jobDef == null ? null : JobMaker.MakeJob(jobDef, t);
        }
    }
}