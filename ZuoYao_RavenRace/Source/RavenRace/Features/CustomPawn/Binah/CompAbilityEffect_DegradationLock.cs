using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class CompProperties_AbilityDegradationLock : CompProperties_AbilityGiveHediff
    {
        public CompProperties_AbilityDegradationLock()
        {
            this.compClass = typeof(CompAbilityEffect_DegradationLock);
        }
    }

    public class CompAbilityEffect_DegradationLock : CompAbilityEffect_GiveHediff
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn p = target.Pawn;
            if (p != null && this.Props.hediffDef != null)
            {
                Hediff h = p.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffDef);
                if (h != null)
                {
                    // 强制设置消失时间
                    var comp = h.TryGetComp<HediffComp_Disappears>();
                    if (comp != null)
                    {
                        comp.ticksToDisappear = 60000; // 1天
                    }
                }
            }
        }
    }
}