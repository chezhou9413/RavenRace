using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    public class CompProperties_AbilityAyayaDash : CompProperties_AbilityEffect
    {
        public ThingDef flyerDef;
        public CompProperties_AbilityAyayaDash() => this.compClass = typeof(CompAbilityEffect_AyayaDash);
    }

    public class CompAbilityEffect_AyayaDash : CompAbilityEffect
    {
        public new CompProperties_AbilityAyayaDash Props => (CompProperties_AbilityAyayaDash)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null) return;

            // 冲刺目标点
            IntVec3 destCell = target.Cell;

            if (Props.flyerDef != null)
            {
                // 创建飞行器
                PawnFlyer flyer = PawnFlyer.MakeFlyer(
                    Props.flyerDef,
                    caster,
                    destCell,
                    null, // 特效
                    null  // 音效
                );

                if (flyer != null)
                {
                    GenSpawn.Spawn(flyer, caster.Position, caster.Map);
                }
            }
        }
    }
}