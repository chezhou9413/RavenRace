using Verse;
using RimWorld;

namespace RavenRace.Features.Servitude
{
    public class ThoughtWorker_HasServant : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var manager = ServitudeManager.Get();
            return manager != null && manager.IsMaster(p) ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }

    public class ThoughtWorker_IsServant : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var manager = ServitudeManager.Get();
            return manager != null && manager.IsServant(p) ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }
}