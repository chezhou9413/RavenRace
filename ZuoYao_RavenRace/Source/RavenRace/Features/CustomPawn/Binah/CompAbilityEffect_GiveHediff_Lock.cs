using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class CompAbilityEffect_GiveHediff_Lock : CompAbilityEffect_GiveHediff
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest); // 先让基类添加 Hediff

            Pawn p = target.Pawn;
            if (p != null)
            {
                // 获取刚刚添加的 Hediff
                Hediff h = p.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffDef);
                if (h != null)
                {
                    // 强制设置消失组件的时间
                    var comp = h.TryGetComp<HediffComp_Disappears>();
                    if (comp != null)
                    {
                        comp.ticksToDisappear = 60000; // 强制1天
                    }
                }
            }
        }
    }
}