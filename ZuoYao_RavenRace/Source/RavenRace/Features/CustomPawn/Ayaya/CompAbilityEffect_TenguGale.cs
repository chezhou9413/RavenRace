using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    public class CompProperties_AbilityTenguGale : CompProperties_AbilityEffect
    {
        public float knockbackDistance = 8f;
        public ThingDef flyerDef;
        public CompProperties_AbilityTenguGale() => this.compClass = typeof(CompAbilityEffect_TenguGale);
    }

    public class CompAbilityEffect_TenguGale : CompAbilityEffect
    {
        public new CompProperties_AbilityTenguGale Props => (CompProperties_AbilityTenguGale)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;

            if (victim == null || caster == null || victim.Dead || victim.Downed) return;
            if (victim.BodySize > 3.0f) return;

            Vector3 pushDir = (victim.DrawPos - caster.DrawPos).normalized;
            Vector3 destPos = victim.DrawPos + pushDir * Props.knockbackDistance;
            IntVec3 destCell = destPos.ToIntVec3();

            destCell = ClampToMap(destCell, caster.Map);
            destCell = FindNearestStandable(destCell, caster.Map);

            if (Props.flyerDef != null)
            {
                PawnFlyer flyer = PawnFlyer.MakeFlyer(
                    Props.flyerDef,
                    victim,
                    destCell,
                    null,
                    null
                );
                if (flyer != null) GenSpawn.Spawn(flyer, victim.Position, victim.Map);
            }
        }

        private IntVec3 ClampToMap(IntVec3 cell, Map map)
        {
            int x = Mathf.Clamp(cell.x, 2, map.Size.x - 3);
            int z = Mathf.Clamp(cell.z, 2, map.Size.z - 3);
            return new IntVec3(x, cell.y, z);
        }

        private IntVec3 FindNearestStandable(IntVec3 cell, Map map)
        {
            if (cell.Standable(map)) return cell;
            return CellFinder.StandableCellNear(cell, map, 5f);
        }
    }
}