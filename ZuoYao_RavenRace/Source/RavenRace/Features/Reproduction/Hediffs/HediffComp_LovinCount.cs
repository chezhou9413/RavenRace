using Verse;
using RimWorld;

namespace RavenRace.Features.Reproduction
{
    public class HediffCompProperties_LovinCount : HediffCompProperties
    {
        public HediffCompProperties_LovinCount()
        {
            this.compClass = typeof(HediffComp_LovinCount);
        }
    }

    /// <summary>
    /// 动态修改 Hediff 名称括号内内容的组件。
    /// 实时从角色的 RecordTracker 中读取交配次数。
    /// </summary>
    public class HediffComp_LovinCount : HediffComp
    {
        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (Pawn == null || Pawn.records == null) return null;

                // 获取具体的交配次数
                int count = Pawn.records.GetAsInt(RavenDefOf.Raven_Record_LovinCount);
                return $"{count}次";
            }
        }
    }
}