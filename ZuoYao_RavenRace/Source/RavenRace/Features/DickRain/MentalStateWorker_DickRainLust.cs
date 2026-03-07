using Verse;
using Verse.AI;

namespace RavenRace.Features.DickRain
{
    public class MentalStateWorker_DickRainLust : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (!base.StateCanOccur(pawn)) return false;
            foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (MentalState_DickRainLust.IsValidTarget(pawn, p))
                    return true;
            }
            return false;
        }
    }
}
