using HarmonyLib;
using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;
using System;

namespace RavenRace.Features.DefenseSystem.Harmony
{
    // =============================================================
    // 强行交配逻辑：全方位拦截战斗 AI
    // =============================================================

    // 拦截 1: 禁止索敌 (最关键的一步)
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "UpdateEnemyTarget")]
    public static class Patch_DisableTargeting
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn)
        {
            // 如果有催情 Buff，禁止更新敌人目标
            if (pawn.health.hediffSet.HasHediff(DefenseDefOf.RavenHediff_AphrodisiacEffect))
            {
                pawn.mindState.enemyTarget = null; // 强行清空
                return false; // 拦截原方法执行
            }
            return true;
        }
    }

    // 拦截 2: 强制分配交配任务
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class Patch_StopFighting
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (pawn.health.hediffSet.HasHediff(DefenseDefOf.RavenHediff_AphrodisiacEffect))
            {
                // 双重保险：清空目标
                pawn.mindState.enemyTarget = null;

                // 寻找对象
                Pawn partner = FindLovinPartner(pawn);
                if (partner != null)
                {
                    // 生成强制交配任务
                    Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_ForceLovin, partner);
                    // 核心参数：防止被轻易打断
                    job.expiryInterval = 20000;
                    job.ignoreDesignations = true;
                    job.checkOverrideOnExpire = false;
                    job.playerForced = true; // 模拟玩家强制，增加优先级

                    __result = job;
                    return false;
                }

                // 找不到对象就发呆
                __result = JobMaker.MakeJob(JobDefOf.Wait_Wander);
                __result.expiryInterval = 120;
                return false;
            }
            return true;
        }

        private static Pawn FindLovinPartner(Pawn pawn)
        {
            // 寻找最近的异性（或同性），无视敌对
            return (Pawn)GenClosest.ClosestThingReachable(
                pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                PathEndMode.Touch, TraverseParms.For(pawn), 40f, // 范围加大
                (t) => t is Pawn p && p != pawn && !p.Downed && !p.Dead && p.RaceProps.Humanlike
            );
        }
    }

    // 拦截 3: 中 Buff 瞬间脱战
    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
    public static class Patch_AddHediff_ClearAggro
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (hediff.def == DefenseDefOf.RavenHediff_AphrodisiacEffect)
            {
                Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn != null)
                {
                    if (pawn.mindState != null)
                    {
                        pawn.mindState.enemyTarget = null;
                        pawn.mindState.meleeThreat = null;
                        pawn.mindState.lastEngageTargetTick = -99999;
                    }
                    // 强力打断当前 Job
                    if (pawn.jobs != null && pawn.CurJob != null)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                    }
                }
            }
        }
    }

    // =============================================================
    // 袖剑断肢逻辑
    // =============================================================
    [HarmonyPatch(typeof(DamageWorker_AddInjury), "Apply")]
    public static class Patch_HiddenBlade_Damage
    {
        [HarmonyPrefix]
        public static void Prefix(ref DamageInfo dinfo, Thing thing)
        {
            Pawn instigator = dinfo.Instigator as Pawn;
            if (instigator == null || dinfo.Weapon?.defName != "Raven_Weapon_HiddenBlade") return;

            if (instigator.health.hediffSet.HasHediff(HediffDef.Named("Raven_Hediff_HiddenBladePrep")))
            {
                if (thing is Pawn p)
                {
                    var limbs = p.health.hediffSet.GetNotMissingParts()
                        .Where(x => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore) ||
                                    x.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbCore))
                        .ToList();

                    if (limbs.Any())
                    {
                        dinfo.SetHitPart(limbs.RandomElement());
                        dinfo.SetAmount(dinfo.Amount * 1.5f);
                    }
                }
            }
        }
    }
}