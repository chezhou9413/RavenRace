using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class Projectile_Piercing : Projectile
    {
        private HashSet<int> hitThings = new HashSet<int>();

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (blockedByShield)
            {
                base.Impact(hitThing, blockedByShield);
                return;
            }

            if (hitThing != null)
            {
                if (hitThings.Contains(hitThing.thingIDNumber)) return;

                if (this.Launcher != null && hitThing.Faction != null && !hitThing.HostileTo(this.Launcher))
                {
                    return;
                }

                if (hitThing == this.Launcher) return;

                DamageInfo dinfo = new DamageInfo(
                    this.def.projectile.damageDef,
                    this.DamageAmount,
                    this.ArmorPenetration,
                    this.ExactRotation.eulerAngles.y,
                    this.Launcher,
                    null,
                    this.def,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    hitThing
                );
                hitThing.TakeDamage(dinfo);
                hitThings.Add(hitThing.thingIDNumber);

                if (hitThing is Pawn)
                {
                    if (this.def.projectile.soundImpact != null)
                    {
                        this.def.projectile.soundImpact.PlayOneShot(new TargetInfo(this.Position, this.Map));
                    }
                    return;
                }

                if (hitThing is Building || hitThing.def.Fillage == FillCategory.Full)
                {
                    Terminate();
                    return;
                }
            }
            else
            {
                Terminate();
            }
        }

        protected virtual void Terminate()
        {
            if (this.def.projectile.explosionRadius > 0f)
            {
                // [修复] 仅使用 XML 定义的声音，如果未定义则为 null (静音/默认)
                // 这样避免了引用不存在的 SoundDefOf.Explosion_Bomb
                SoundDef sound = this.def.projectile.soundExplode;

                GenExplosion.DoExplosion(
                    center: this.Position,
                    map: this.Map,
                    radius: this.def.projectile.explosionRadius,
                    damType: this.def.projectile.damageDef,
                    instigator: this.Launcher,
                    damAmount: this.DamageAmount,
                    armorPenetration: this.ArmorPenetration,
                    explosionSound: sound,
                    weapon: this.equipmentDef,
                    projectile: this.def,
                    intendedTarget: this.intendedTarget.Thing,
                    postExplosionSpawnThingDef: this.def.projectile.postExplosionSpawnThingDef,
                    postExplosionSpawnChance: this.def.projectile.postExplosionSpawnChance,
                    postExplosionSpawnThingCount: this.def.projectile.postExplosionSpawnThingCount,
                    postExplosionGasType: this.def.projectile.postExplosionGasType,
                    postExplosionGasRadiusOverride: null,
                    postExplosionGasAmount: 255,
                    applyDamageToExplosionCellsNeighbors: this.def.projectile.applyDamageToExplosionCellsNeighbors
                );
            }
            else
            {
                GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);
                if (this.def.projectile.landedEffecter != null)
                {
                    this.def.projectile.landedEffecter.Spawn(this.Position, this.Map, 1f).Cleanup();
                }
                if (this.def.projectile.soundImpact != null)
                {
                    this.def.projectile.soundImpact.PlayOneShot(new TargetInfo(this.Position, this.Map));
                }
            }
            this.Destroy(DestroyMode.Vanish);
        }
    }
}