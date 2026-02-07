using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace
{
    public class WorkGiver_FillAltar : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_AltarInfuser>();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_AltarInfuser infuser = t as Building_AltarInfuser;
            if (infuser == null) return false;

            if (infuser.targetDef == null) return false;
            if (infuser.innerContainer.Count > 0) return false;
            if (infuser.IsForbidden(pawn)) return false;

            if (!pawn.CanReserve(infuser, 1, -1, null, forced)) return false;

            Thing item = FindBestItem(pawn, infuser.targetDef);
            if (item == null)
            {
                if (forced)
                {
                    // [Fixed] 简单拼接，防止红字
                    string msg = "缺少: " + infuser.targetDef.LabelCap;
                    JobFailReason.Is(msg);
                }
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_AltarInfuser infuser = t as Building_AltarInfuser;
            Thing item = FindBestItem(pawn, infuser.targetDef);
            if (item == null) return null;

            Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_FillAltar, item, infuser);
            job.count = 1;
            return job;
        }

        private Thing FindBestItem(Pawn pawn, ThingDef targetDef)
        {
            return GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(targetDef),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn),
                9999f,
                (Thing t) => !t.IsForbidden(pawn) && pawn.CanReserve(t)
            );
        }
    }
}