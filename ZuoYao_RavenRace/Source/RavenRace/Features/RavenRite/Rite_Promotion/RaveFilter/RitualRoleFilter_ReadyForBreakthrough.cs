using RavenRace.Features.RavenRite.Rite_Promotion.Purification.Comps;
using Verse;

namespace RavenRace.Features.RavenRite.CustomRiteCore.RaveFilter
{
    // 要求Pawn的金乌浓度已达到当前纯化阶段的阈值上限
    public class RitualRoleFilter_ReadyForBreakthrough : RitualRoleFilter
    {
        public override bool CanAssign(Pawn pawn)
        {
            var comp = pawn.TryGetComp<CompPurification>();
            if (comp == null) return false;

            float limit = comp.GetMaxConcentrationLimit();
            return comp.GoldenCrowConcentration >= limit;
        }

        public override string GetDisabledReason(Pawn pawn)
        {
            var comp = pawn.TryGetComp<CompPurification>();
            if (comp == null) return "该 Pawn 没有纯化组件";

            float current = comp.GoldenCrowConcentration;
            float limit = comp.GetMaxConcentrationLimit();
            return "金乌浓度尚未达到突破阈值（当前 " + current.ToStringPercent("F0")
                 + " / 需要 " + limit.ToStringPercent("F0") + "）";
        }
    }
}
