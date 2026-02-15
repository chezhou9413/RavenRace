using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class CompProperties_AbilityShockwave : CompProperties_AbilityEffect
    {
        public float radius = 20f;
        public int damageAmount = 500;
        public CompProperties_AbilityShockwave()
        {
            this.compClass = typeof(CompAbilityEffect_Shockwave);
        }
    }

    public class CompAbilityEffect_Shockwave : CompAbilityEffect
    {
        public new CompProperties_AbilityShockwave Props => (CompProperties_AbilityShockwave)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = this.parent.pawn;
            Map map = caster.Map;
            IntVec3 center = caster.Position;

            // 1. AOE 伤害 (排除自己)
            List<Thing> targets = new List<Thing>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, Props.radius, true))
            {
                if (!cell.InBounds(map)) continue;
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t != caster && (t.def.category == ThingCategory.Pawn || t.def.category == ThingCategory.Building))
                    {
                        if (!targets.Contains(t)) targets.Add(t);
                    }
                }
            }

            foreach (Thing t in targets)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, Props.damageAmount, 0.5f, -1, caster);
                t.TakeDamage(dinfo);
            }

            // 2. 视觉特效
            if (map != null)
            {
                FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, 10f);
                // 使用原版冲击波
                if (FleckDefOf.PsycastAreaEffect != null)
                {
                    FleckMaker.Static(center, map, FleckDefOf.PsycastAreaEffect, Props.radius * 2f);
                }
                else
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Vector3 rndPos = center.ToVector3() + Vector3Utility.FromAngleFlat(Rand.Range(0, 360)) * (Props.radius * Rand.Range(0.2f, 1f));
                        FleckMaker.ThrowDustPuff(rndPos, map, 2f);
                    }
                }
            }

            Find.CameraDriver.shaker.DoShake(3.0f);
        }
    }
}