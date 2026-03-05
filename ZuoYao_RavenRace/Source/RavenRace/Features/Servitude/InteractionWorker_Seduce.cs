using RimWorld;
using Verse;

namespace RavenRace.Features.Servitude
{
    public class InteractionWorker_Seduce : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            // 这个互动不应该由AI随机选择，而是由我们的JobGiver强制触发，所以权重为0
            return 0f;
        }
    }
}