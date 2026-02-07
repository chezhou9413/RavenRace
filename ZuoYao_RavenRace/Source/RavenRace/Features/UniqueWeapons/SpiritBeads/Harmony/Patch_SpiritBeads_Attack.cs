using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace RavenRace.Features.UniqueWeapons.SpiritBeads.Harmony
{
    /// <summary>
    /// 拦截原版近战攻击逻辑。
    /// 如果使用的是灵卵拉珠且处于纳刀状态，则执行特殊攻击。
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Patch_SpiritBeads_Attack
    {
        [HarmonyPrefix]
        public static bool Prefix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target, ref DamageWorker.DamageResult __result)
        {
            // 1. 检查武器是否为灵卵拉珠
            if (__instance.EquipmentSource == null || __instance.EquipmentSource.def.defName != "Raven_Weapon_SpiritBeads")
            {
                return true; // 执行原版逻辑
            }

            // 2. 检查组件状态 (是否插入)
            var comp = __instance.EquipmentSource.GetComp<CompSpiritBeads>();
            if (comp == null || !comp.IsInserted)
            {
                return true; // 未插入，执行原版普通攻击
            }

            // --- 执行特殊攻击逻辑 (拔刀斩) ---

            Pawn caster = __instance.CasterPawn;
            DamageWorker.DamageResult result = new DamageWorker.DamageResult();
            Pawn targetPawn = target.Thing as Pawn;

            // 计算伤害 (40点高伤钝器)
            float damageAmount = 40f;

            // [Fixed CS1503] 构造函数修正：传入 EquipmentSource.def (ThingDef)
            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.Blunt,
                damageAmount,
                2.0f, // 高穿甲
                -1,
                caster,
                null,
                __instance.EquipmentSource.def, // 关键修正：这里必须传 ThingDef
                DamageInfo.SourceCategory.ThingOrUnknown,
                null,
                true, // instigatorGuilty
                true  // spawnFilth
            );

            // 造成伤害
            if (target.Thing != null)
            {
                result = target.Thing.TakeDamage(dinfo);
            }

            // 命中特效与状态
            if (targetPawn != null && !targetPawn.Dead)
            {
                // 晕眩 3秒
                if (targetPawn.stances != null && targetPawn.stances.stunner != null)
                {
                    targetPawn.stances.stunner.StunFor(180, caster, true, true);
                }

                // 施加高潮 Debuff
                HediffDef climaxDef = SpiritBeadsDefOf.Raven_Hediff_HighClimax;
                if (climaxDef != null)
                {
                    targetPawn.health.AddHediff(climaxDef);
                    Hediff h = targetPawn.health.hediffSet.GetFirstHediffOfDef(climaxDef);
                    if (h != null) h.Severity = 1.0f;
                }

                // 视觉文字
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "RavenRace_Text_ClimaxImpact".Translate(), 3f);
            }

            // 拔出音效
            SoundDef popSound = DefDatabase<SoundDef>.GetNamedSilentFail("Hive_Spawn");
            popSound?.PlayOneShot(new TargetInfo(caster.Position, caster.Map));

            // 改变状态：拔出
            comp.SetInserted(caster, false);

            // 返回结果并拦截原方法
            __result = result;
            return false;
        }
    }
}