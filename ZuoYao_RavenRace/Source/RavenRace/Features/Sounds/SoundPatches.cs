using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace RavenRace.Features.Sounds
{
    /// <summary>
    /// 统一管理所有整蛊音效的Harmony补丁。
    /// </summary>
    [HarmonyPatch]
    public static class SoundPatches
    {
        // 1. 受击音效
        // 【最终修复】将补丁目标改为更底层的 Verse.Thing 类，并修正了参数顺序。
        [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
        [HarmonyPostfix]
        public static void TakeDamage_Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
        {
            // 补丁作用于所有Thing，所以必须先判断类型是否为Pawn
            if (!(__instance is Pawn pawn))
            {
                return;
            }

            // 只对渡鸦族生效，且造成了实际伤害
            if (pawn.def == RavenDefOf.Raven_Race && __result != null && __result.totalDamageDealt > 0.1f)
            {
                if (Rand.Chance(0.1f)) // 10%概率触发
                {
                    RavenSoundDefOf.RavenMeme_TakeDamage?.PlayOneShot(SoundInfo.InMap(new TargetInfo(pawn)));
                }
            }
        }

        // 2. Binah技能音效
        [HarmonyPatch(typeof(Verb_CastAbility), "TryCastShot")]
        [HarmonyPrefix]
        public static void CastAbility_Prefix(Verb_CastAbility __instance)
        {
            // 【修复】使用正确的 DefOf 引用
            if (__instance.CasterPawn?.kindDef == Features.CustomPawn.Binah.BinahDefOf.Raven_PawnKind_Binah)
            {
                RavenSoundDefOf.RavenMeme_BinahAbility?.PlayOneShot(SoundInfo.InMap(new TargetInfo(__instance.CasterPawn)));
            }
        }

        // 3. 倒地音效
        [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
        [HarmonyPostfix]
        public static void MakeDowned_Postfix(Pawn_HealthTracker __instance, Pawn ___pawn)
        {
            if (___pawn != null && ___pawn.def == RavenDefOf.Raven_Race)
            {
                RavenSoundDefOf.RavenMeme_PawnDowned?.PlayOneShot(SoundInfo.InMap(new TargetInfo(___pawn)));
            }
        }

        // 4. 社交/求爱失败音效
        [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.Interacted))]
        [HarmonyPostfix]
        public static void RomanceAttempt_Postfix(Pawn initiator, Pawn recipient)
        {
            // 检查发起者是否为渡鸦族，并且是否刚刚被拒绝
            if (initiator.def == RavenDefOf.Raven_Race && initiator.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(ThoughtDefOf.RebuffedMyRomanceAttempt) != null)
            {
                RavenSoundDefOf.RavenMeme_SocialFail?.PlayOneShot(SoundInfo.InMap(new TargetInfo(initiator)));
            }
        }

        // 5. 制作失败音效
        [HarmonyPatch(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn), new[] { typeof(int), typeof(bool) })]
        [HarmonyPostfix]
        public static void CraftFail_Postfix(ref QualityCategory __result, int relevantSkillLevel)
        {
            if (__result == QualityCategory.Awful)
            {
                // 通过技能等级判断，高等级工匠失败时更有戏剧性
                if (Rand.Chance(Mathf.Lerp(0.1f, 0.8f, (float)relevantSkillLevel / 20f)))
                {
                    RavenSoundDefOf.RavenMeme_CraftFail?.PlayOneShotOnCamera();
                }
            }
        }

        // 6. 建造失败音效
        [HarmonyPatch(typeof(Frame), nameof(Frame.FailConstruction))]
        [HarmonyPrefix]
        public static void BuildFail_Prefix(Frame __instance, Pawn worker)
        {
            if (worker != null && worker.def == RavenDefOf.Raven_Race && Rand.Chance(0.2f))
            {
                RavenSoundDefOf.RavenMeme_CraftFail?.PlayOneShot(SoundInfo.InMap(new TargetInfo(worker)));
            }
        }

        // 7. 被侮辱音效
        [HarmonyPatch(typeof(InteractionWorker), nameof(InteractionWorker.Interacted))]
        [HarmonyPostfix]
        public static void Insulted_Postfix(InteractionWorker __instance, Pawn initiator, Pawn recipient)
        {
            if (__instance is InteractionWorker_Insult && recipient.def == RavenDefOf.Raven_Race)
            {
                RavenSoundDefOf.RavenMeme_Insulted?.PlayOneShot(SoundInfo.InMap(new TargetInfo(recipient)));
            }
        }

        // 8. 死亡音效
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        [HarmonyPostfix]
        // 【修复】Pawn.Kill方法可能没有参数，或者有一个可选的DamageInfo? dinfo。为了兼容性，我们只捕获实例。
        public static void Kill_Postfix(Pawn __instance)
        {
            // 只对玩家殖民地的渡鸦播放
            if (__instance.def == RavenDefOf.Raven_Race && __instance.Faction == Faction.OfPlayer && __instance.MapHeld != null)
            {
                RavenSoundDefOf.RavenMeme_PawnDeath?.PlayOneShot(SoundInfo.InMap(new TargetInfo(__instance.Position, __instance.MapHeld)));
            }
        }

        // 9. 逃跑音效
        [HarmonyPatch(typeof(JobDriver_Flee), "MakeNewToils")]
        [HarmonyPostfix]
        public static IEnumerable<Toil> Flee_Postfix(IEnumerable<Toil> values, JobDriver_Flee __instance)
        {
            bool soundPlayed = false;
            foreach (var toil in values)
            {
                if (!soundPlayed && __instance.pawn.def == RavenDefOf.Raven_Race)
                {
                    var originalInit = toil.initAction;
                    toil.initAction = () =>
                    {
                        originalInit?.Invoke();
                        RavenSoundDefOf.RavenMeme_Fleeing?.PlayOneShot(SoundInfo.InMap(new TargetInfo(__instance.pawn)));
                    };
                    soundPlayed = true; //确保只在第一个toil播放
                }
                yield return toil;
            }
        }
    }
}