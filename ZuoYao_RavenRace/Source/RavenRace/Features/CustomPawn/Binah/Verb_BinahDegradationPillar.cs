using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class Verb_BinahDegradationPillar : Verb_CastAbility
    {
        // [Fix] 不使用 override，定义自己的属性
        public ThingDef MyProjectile => BinahDefOf.Raven_Projectile_Binah_Degradation;

        protected override bool TryCastShot()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map) return false;

            // [Fix] 使用 BinahDefOf
            ThingDef projDef = MyProjectile;
            if (projDef == null) return false;

            Vector3 drawPos = caster.DrawPos;
            Projectile projectile = (Projectile)GenSpawn.Spawn(projDef, caster.Position, caster.Map, WipeMode.Vanish);

            projectile.Launch(caster, drawPos, currentTarget, currentTarget, ProjectileHitFlags.IntendedTarget, false, null);

            return true;
        }
    }
}