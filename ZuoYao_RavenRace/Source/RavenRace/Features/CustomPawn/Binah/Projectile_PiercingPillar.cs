using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RavenRace.Features.CustomPawn.Binah
{
    /// <summary>
    /// 穿透性柱状投射物
    /// </summary>
    public class Projectile_PiercingPillar : Projectile
    {
        // 缓存已击中的目标ID (使用 int ID 以提高性能并匹配类型)
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
                // [修复] 使用 thingIDNumber (int) 而非 ThingID (string)
                if (hitThings.Contains(hitThing.thingIDNumber)) return;

                int damageAmount = this.DamageAmount;
                float armorPenetration = this.ArmorPenetration;

                DamageDef damageDef = this.def.projectile.damageDef;

                // 记录战斗日志
                BattleLogEntry_RangedImpact logEntry = new BattleLogEntry_RangedImpact(
                    this.launcher,
                    hitThing,
                    this.intendedTarget.Thing,
                    this.equipmentDef,
                    this.def,
                    null // coverDef
                );
                Find.BattleLog.Add(logEntry); // 记得添加日志到数据库

                // 造成伤害
                DamageInfo dinfo = new DamageInfo(
                    damageDef,
                    damageAmount,
                    armorPenetration,
                    this.ExactRotation.eulerAngles.y,
                    this.launcher,
                    null,
                    this.def,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    this.intendedTarget.Thing
                );

                hitThing.TakeDamage(dinfo).AssociateWithLog(logEntry);

                // [修复] 加入 int 类型的 ID
                hitThings.Add(hitThing.thingIDNumber);

                if (this.def.projectile.soundHitThickRoof != null)
                {
                    this.def.projectile.soundHitThickRoof.PlayOneShot(this);
                }
            }
        }

        // 保持 protected 以匹配基类
        protected override void Tick()
        {
            base.Tick();

            if (!this.Position.InBounds(this.Map))
            {
                this.Destroy(DestroyMode.Vanish);
            }
        }
    }
}