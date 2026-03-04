using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class CompProperties_AbilityShockwave : CompProperties_AbilityEffect
    {
        public float radius = 30f;
        public int damageAmount = 500;
        public CompProperties_AbilityShockwave() => this.compClass = typeof(CompAbilityEffect_Shockwave);
    }

    public class CompAbilityEffect_Shockwave : CompAbilityEffect
    {
        public new CompProperties_AbilityShockwave Props => (CompProperties_AbilityShockwave)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Map map = caster.Map;

            Vector3 centerPos = caster.DrawPos;
            centerPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            // 1. 金色冲击波 Mote
            ThingDef moteDef = BinahDefOf.Raven_Mote_Binah_ShockwaveDistortion ?? ThingDefOf.Mote_PowerBeam;

            // 初始 Scale 设为 0.1，让它从中心扩散
            Mote mote = MoteMaker.MakeStaticMote(centerPos, map, moteDef, 0.1f);
            if (mote != null)
            {
                mote.exactPosition = centerPos;
                mote.instanceColor = new Color(1f, 0.85f, 0.1f, 0.9f); // 金色
                mote.solidTimeOverride = 2.0f; // 持续2秒，配合 XML 中的 growthRate 变慢
            }

            // 2. 屏幕扭曲
            Effecter effecter = new Effecter(EffecterDefOf.Skip_Entry);
            effecter.Trigger(new TargetInfo(caster.Position, map), new TargetInfo(caster.Position, map));
            effecter.Cleanup();

            // 3. 屏幕震动
            Find.CameraDriver.shaker.DoShake(3.0f);

            // 4. 伤害逻辑：【修复】震击只对 Pawn 造成伤害，不破坏建筑
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, Props.radius, true))
            {
                if (!cell.InBounds(map)) continue;

                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing t = things[i];

                    // 跳过施法者自身
                    if (t == caster) continue;

                    // 跳过友方 Pawn
                    if (t is Pawn p && !p.HostileTo(caster)) continue;

                    // 【修复】只对 Pawn 造成伤害，移除对 Building 的判断
                    if (t is Pawn)
                    {
                        t.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Props.damageAmount, 0.5f, -1, caster));

                        // 对存活的 Pawn 施加击晕
                        Pawn victim = (Pawn)t;
                        if (!victim.Dead)
                        {
                            victim.stances.stunner.StunFor(120, caster);
                        }
                    }
                }
            }


        }
    }
}