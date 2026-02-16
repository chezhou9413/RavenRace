using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class CompProperties_AbilityDegradationPillar : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityDegradationPillar()
        {
            this.compClass = typeof(CompAbilityEffect_DegradationPillar);
        }
    }

    public class CompAbilityEffect_DegradationPillar : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned) return;

            ThingDef projDef = BinahDefOf.Raven_Projectile_Binah_Degradation;
            Projectile proj = (Projectile)GenSpawn.Spawn(projDef, caster.Position, caster.Map);

            proj.Launch(
                caster,
                caster.DrawPos,
                target,
                target,
                ProjectileHitFlags.All
            );
        }
    }
}