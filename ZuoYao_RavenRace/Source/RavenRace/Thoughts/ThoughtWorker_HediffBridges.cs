using System;
using Verse;
using RimWorld;
using RavenRace.Features.UniqueWeapons.SpiritBeads; // 引用灵珠DefOf

namespace RavenRace.Thoughts
{
    // 1. 乐高之痛检测器
    public class ThoughtWorker_LegoPain : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.health == null || p.health.hediffSet == null) return ThoughtState.Inactive;

            // 检查是否有乐高疼痛 Hediff
            if (DefenseDefOf.RavenHediff_LegoPain != null &&
                p.health.hediffSet.HasHediff(DefenseDefOf.RavenHediff_LegoPain))
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.Inactive;
        }
    }

    // 2. 跟腱断裂检测器
    public class ThoughtWorker_TendonCut : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.health == null || p.health.hediffSet == null) return ThoughtState.Inactive;

            if (DefenseDefOf.RavenHediff_TendonCut != null &&
                p.health.hediffSet.HasHediff(DefenseDefOf.RavenHediff_TendonCut))
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.Inactive;
        }
    }

    // 3. 催情效果检测器
    public class ThoughtWorker_AphrodisiacEffect : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.health == null || p.health.hediffSet == null) return ThoughtState.Inactive;

            // 只有严重程度 > 0.1 (aroused阶段) 才显示想法和表情
            if (DefenseDefOf.RavenHediff_AphrodisiacEffect != null)
            {
                Hediff h = p.health.hediffSet.GetFirstHediffOfDef(DefenseDefOf.RavenHediff_AphrodisiacEffect);
                if (h != null && h.Severity > 0.1f)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
            }
            return ThoughtState.Inactive;
        }
    }

    // 4. 绝顶升天检测器
    public class ThoughtWorker_HighClimax : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.health == null || p.health.hediffSet == null) return ThoughtState.Inactive;

            if (SpiritBeadsDefOf.Raven_Hediff_HighClimax != null &&
                p.health.hediffSet.HasHediff(SpiritBeadsDefOf.Raven_Hediff_HighClimax))
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.Inactive;
        }
    }
}