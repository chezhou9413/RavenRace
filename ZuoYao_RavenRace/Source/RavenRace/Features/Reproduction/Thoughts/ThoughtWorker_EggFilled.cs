using Verse;
using RimWorld;
using RavenRace.Features.Reproduction;

namespace RavenRace.Features.Reproduction // [Change] Namespace
{
    /// <summary>
    /// 灵卵填充想法检测器
    /// </summary>
    public class ThoughtWorker_EggFilled : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(RavenDefOf.Raven_Hediff_SpiritEggInserted);
            if (hediff == null) return ThoughtState.Inactive;

            // [Change] 使用新类名
            HediffCompSpiritEggHolder holder = hediff.TryGetComp<HediffCompSpiritEggHolder>();
            if (holder == null || holder.innerContainer.Count == 0) return ThoughtState.Inactive;

            // [Change] 使用新类名
            CompSpiritEgg eggComp = holder.innerContainer[0].TryGetComp<CompSpiritEgg>();
            if (eggComp == null) return ThoughtState.ActiveAtStage(0);

            bool isParent = (eggComp.fatherId == p.ThingID || eggComp.motherId == p.ThingID);

            return ThoughtState.ActiveAtStage(isParent ? 1 : 0);
        }
    }
}