using RimWorld;
using Verse;
using Verse.AI;

namespace RavenRace.Features.DickRain
{
    [DefOf]
    public static class DickRainJobDefOf
    {
        public static JobDef DickRain_YuwangNanRen;
    }

    public class MentalState_DickRainLust : MentalState
    {
        public bool hasActed;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hasActed, "hasActed", false);
        }

        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            TryStartActJob();
        }

        public override void MentalStateTick(int delta)
        {
            if (!hasActed && pawn.IsHashIntervalTick(300, delta))
            {
                if (pawn.CurJobDef != DickRainJobDefOf.DickRain_YuwangNanRen)
                    TryStartActJob();
            }
            base.MentalStateTick(delta);
        }

        public void Notify_ActCompleted()
        {
            hasActed = true;
        }

        private void TryStartActJob()
        {
            Pawn target = FindTarget();
            if (target == null)
            {
                RecoverFromState();
                return;
            }
            Job job = JobMaker.MakeJob(DickRainJobDefOf.DickRain_YuwangNanRen, target);
            pawn.jobs.StartJob(job, JobCondition.InterruptForced,
                resumeCurJobAfterwards: false,
                cancelBusyStances: true);
        }

        private Pawn FindTarget()
        {
            Pawn best = null;
            float bestDist = float.MaxValue;
            foreach (Pawn candidate in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (!IsValidTarget(pawn, candidate)) continue;
                if (!pawn.CanReserve(candidate)) continue;
                if (IsInvolvedInJob(candidate)) continue;
                float dist = pawn.Position.DistanceToSquared(candidate.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }
            return best;
        }

        private static bool IsInvolvedInJob(Pawn candidate)
        {
            if (candidate.CurJobDef == DickRainJobDefOf.DickRain_YuwangNanRen)
                return true;

            if (candidate.CurJob?.targetA.Pawn == candidate)
                return true;

            foreach (Pawn other in candidate.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == candidate) continue;
                if (other.CurJobDef != DickRainJobDefOf.DickRain_YuwangNanRen) continue;
                if (other.CurJob?.targetA.Pawn == candidate) return true;
            }

            return false;
        }

        public static bool IsValidTarget(Pawn actor, Pawn candidate)
        {
            if (candidate == null || candidate == actor) return false;
            if (candidate.Dead || candidate.Downed) return false;
            if (!candidate.Spawned) return false;
            if (!candidate.Awake()) return false;
            if (!candidate.RaceProps.IsFlesh) return false;
            if (!actor.CanReserveAndReach(candidate, PathEndMode.Touch, Danger.Deadly)) return false;
            return true;
        }
    }
}