using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class CompProperties_PathDamage : CompProperties
    {
        public CompProperties_PathDamage()
        {
            this.compClass = typeof(CompProjectile_PathDamage);
        }
    }

    public class CompProjectile_PathDamage : ThingComp
    {
        private Vector3 lastPos;
        private HashSet<int> hitThings = new HashSet<int>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            lastPos = parent.DrawPos;
        }

        public override void CompTick()
        {
            base.CompTick();

            Projectile proj = parent as Projectile;
            if (proj == null || proj.Map == null) return;

            Vector3 currentPos = proj.DrawPos;

            if (proj.def == BinahDefOf.Raven_Projectile_Binah_Degradation)
            {
                if (Find.TickManager.TicksGame % 2 == 0)
                {
                    ThingDef fairyDef = BinahDefOf.Raven_Mote_Binah_FairyTrail;
                    if (fairyDef != null)
                    {
                        Mote mote = MoteMaker.MakeStaticMote(currentPos, proj.Map, fairyDef, 1.2f);
                        if (mote != null)
                        {
                            mote.exactPosition += new Vector3(Rand.Range(-0.2f, 0.2f), 0, Rand.Range(-0.2f, 0.2f));
                            mote.rotationRate = Rand.Range(-10f, 10f);
                            mote.solidTimeOverride = 0.3f;
                        }
                    }
                }
            }

            if (lastPos == currentPos) return;

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(lastPos.ToIntVec3(), currentPos.ToIntVec3()))
            {
                if (!cell.InBounds(proj.Map)) continue;

                List<Thing> things = cell.GetThingList(proj.Map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing t = things[i];
                    if (t is Pawn p && !hitThings.Contains(t.thingIDNumber))
                    {
                        if (proj.Launcher != null && !p.HostileTo(proj.Launcher)) continue;
                        if (p == proj.Launcher) continue;

                        DamageInfo dinfo = new DamageInfo(
                            proj.def.projectile.damageDef,
                            proj.DamageAmount,
                            proj.ArmorPenetration,
                            proj.ExactRotation.eulerAngles.y,
                            proj.Launcher,
                            null,
                            proj.def
                        );
                        t.TakeDamage(dinfo);
                        hitThings.Add(t.thingIDNumber);

                        if (proj.def.projectile.soundImpact != null)
                        {
                            proj.def.projectile.soundImpact.PlayOneShot(new TargetInfo(cell, proj.Map));
                        }
                    }
                }
            }

            lastPos = currentPos;
        }
    }
}