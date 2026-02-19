using HarmonyLib;
using RimWorld;
using Verse;

namespace RavenRace.Compat.Miraboreas
{
    /// <summary>
    /// Harmony补丁，用于实现米拉波雷亚斯血脉的近战范围攻击（拍击）效果。
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Patch_MiraboreasSlam
    {
        /// <summary>
        /// 在原版造成近战伤害后执行。
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
        {
            // 1. 检查Mod设置是否启用
            if (!RavenRaceMod.Settings.enableMiraboreasCompat) return;

            // 2. 获取攻击者和主要目标
            Pawn attacker = __instance.CasterPawn;
            Thing mainTarget = target.Thing;

            // 3. 基础条件检查
            if (attacker == null || attacker.Dead || mainTarget == null || attacker.Map == null) return;

            // 4. 检查攻击者是否为渡鸦族，且装备了近战武器
            if (attacker.def != RavenDefOf.Raven_Race || attacker.equipment?.Primary == null || !attacker.equipment.Primary.def.IsMeleeWeapon) return;

            // 5. 检查攻击者是否有“黑龙血脉”Hediff
            Hediff hediff = attacker.health?.hediffSet.GetFirstHediffOfDef(MiraboreasCompatUtility.MiraboreasBloodlineHediff);
            if (hediff == null) return;

            // 6. 获取Hediff上的拍击效果组件
            CompMiraboreasSlam comp = hediff.TryGetComp<CompMiraboreasSlam>();
            if (comp == null) return;

            // 7. 获取拍击参数
            float slamRadius = comp.Props.slamRadius;
            float slamDamageFactor = comp.Props.slamDamageFactor;

            // 8. 查找主要目标周围的其他敌人
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(mainTarget.Position, attacker.Map, slamRadius, true))
            {
                // 跳过主要目标、攻击者自己以及非Pawn目标
                if (thing == mainTarget || thing == attacker || !(thing is Pawn secondaryPawn))
                {
                    continue;
                }

                // 只对敌对且未倒地的Pawn造成伤害
                if (secondaryPawn.HostileTo(attacker) && !secondaryPawn.Downed)
                {
                    // 9. 计算范围伤害
                    // 复刻米拉波雷亚斯原版逻辑：伤害和穿透都乘以伤害系数
                    float damage = __instance.verbProps.AdjustedMeleeDamageAmount(__instance, attacker) * slamDamageFactor;
                    float armorPen = __instance.verbProps.AdjustedArmorPenetration(__instance, attacker) * slamDamageFactor;
                    DamageDef damageDef = __instance.verbProps.meleeDamageDef ?? DamageDefOf.Blunt;

                    // 10. 创建并应用伤害
                    DamageInfo dinfo = new DamageInfo(
                        damageDef,
                        damage,
                        armorPen,
                        -1f,
                        attacker,
                        null,
                        __instance.EquipmentSource?.def ?? attacker.def);

                    // 设置伤害角度
                    dinfo.SetAngle((secondaryPawn.Position - attacker.Position).ToVector3());

                    secondaryPawn.TakeDamage(dinfo);
                }
            }
        }
    }
}