using System.Net;
using HarmonyLib;
using RavenRace.Features.DegradationCharm; // 引用淫堕符咒相关定义
using RimWorld;
using rjw; // 必须引用 RJW
using UnityEngine;
using Verse;

namespace RavenRace.RJWCompat
{
    /// <summary>
    /// RJW 性爱结算补丁
    /// 作用：
    /// 1. 恢复因拦截 Raven_Job_ForceLovin 而丢失的“降低抵抗/意志”逻辑。
    /// 2. 恢复因未使用 JobDriver_Lovin 而丢失的“增加淫堕条”逻辑。
    /// 3. [新增] 向原版 Record 统计系统中注入交配次数，以联动“繁衍至上”文化。
    /// </summary>
    [HarmonyPatch(typeof(SexUtility), "Aftersex")]
    public static class Patch_RJW_AfterSex
    {
        [HarmonyPostfix]
        public static void Postfix(SexProps props)
        {
            // 安全检查
            if (props == null || props.pawn == null || props.partner == null) return;

            Pawn initiator = props.pawn; // 发起者
            Pawn partner = props.partner; // 承受者

            // ------------------------------------------------------
            // 1. 处理“淫堕刻印” (Degradation Charm) 增加逻辑
            // ------------------------------------------------------
            IncreaseDegradationIfPresent(initiator);
            IncreaseDegradationIfPresent(partner);

            // ------------------------------------------------------
            // 2. [核心新增] 记录交配次数至原版 Record，用于驱动繁衍文化Hediff
            // ------------------------------------------------------
            RecordLovinCount(initiator);
            RecordLovinCount(partner);

            // ------------------------------------------------------
            // 3. 处理“强制求爱”带来的囚犯互动 (降低抵抗/意志)
            // ------------------------------------------------------
            if (initiator.def == DefDatabase<ThingDef>.GetNamedSilentFail("Raven_Race") && partner.IsPrisonerOfColony)
            {
                HandleRavenPrisonerInteraction(initiator, partner);
            }
        }

        /// <summary>
        /// 安全地为目标增加一次交配记录
        /// </summary>
        private static void RecordLovinCount(Pawn pawn)
        {
            // 通过反射调用主模组的防抖工具方法，以彻底防止 RJW 双向回调导致的重复增加
            var utilityType = GenTypes.GetTypeInAnyAssembly("RavenRace.Features.Reproduction.RavenReproductionUtility");
            if (utilityType != null)
            {
                var method = utilityType.GetMethod("AddLovinCountSafely", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, new object[] { pawn });
                }
            }
        }

        /// <summary>
        /// 增加淫堕 Hediff 的严重度
        /// </summary>
        private static void IncreaseDegradationIfPresent(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;

            HediffDef degDef = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_Degradation");
            if (degDef == null) return;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(degDef);
            if (hediff != null)
            {
                hediff.Severity += 0.10f;
            }
        }

        /// <summary>
        /// 处理囚犯互动逻辑
        /// </summary>
        private static void HandleRavenPrisonerInteraction(Pawn initiator, Pawn prisoner)
        {
            // 因为 RJW 命名空间隔离，此处无法直接访问 RavenRaceMod.Settings
            // 采用动态反射获取设置参数，确保解耦
            var settingsType = GenTypes.GetTypeInAnyAssembly("RavenRace.RavenRaceSettings");
            if (settingsType == null) return;

            var modType = GenTypes.GetTypeInAnyAssembly("RavenRace.RavenRaceMod");
            var settingsProp = modType?.GetProperty("Settings");
            var settings = settingsProp?.GetValue(null);
            if (settings == null) return;

            float forceLovinResistanceReduction = (float)settingsType.GetField("forceLovinResistanceReduction").GetValue(settings);
            float forceLovinWillReduction = (float)settingsType.GetField("forceLovinWillReduction").GetValue(settings);
            float forceLovinCertaintyReduction = (float)settingsType.GetField("forceLovinCertaintyReduction").GetValue(settings);
            float forceLovinInstantRecruitChance = (float)settingsType.GetField("forceLovinInstantRecruitChance").GetValue(settings);
            float forceLovinBreakLoyaltyChance = (float)settingsType.GetField("forceLovinBreakLoyaltyChance").GetValue(settings);

            bool graphicsDirty = false;

            if (prisoner.guest.ExclusiveInteractionMode == PrisonerInteractionModeDefOf.AttemptRecruit && prisoner.guest.resistance > 0)
            {
                prisoner.guest.resistance = Mathf.Max(0, prisoner.guest.resistance - forceLovinResistanceReduction);
                Messages.Message($"[渡鸦] 囚犯的抵抗被极乐削弱了 {forceLovinResistanceReduction}。", prisoner, MessageTypeDefOf.PositiveEvent);

                if (prisoner.guest.resistance <= 0 && Rand.Chance(forceLovinInstantRecruitChance))
                {
                    InteractionWorker_RecruitAttempt.DoRecruit(initiator, prisoner);
                    AddDominatedRelation(prisoner, initiator);
                    Messages.Message($"[渡鸦] 极乐让 {prisoner.LabelShort} 彻底臣服并加入了殖民地！", prisoner, MessageTypeDefOf.PositiveEvent);
                    graphicsDirty = true;
                }
            }
            else if (prisoner.guest.ExclusiveInteractionMode == PrisonerInteractionModeDefOf.Enslave && prisoner.guest.will > 0)
            {
                prisoner.guest.will = Mathf.Max(0, prisoner.guest.will - forceLovinWillReduction);
                Messages.Message($"[渡鸦] 囚犯的意志被肉欲瓦解了 {forceLovinWillReduction}。", prisoner, MessageTypeDefOf.PositiveEvent);

                if (prisoner.guest.will <= 0 && Rand.Chance(forceLovinInstantRecruitChance))
                {
                    GenGuest.EnslavePrisoner(initiator, prisoner);
                    AddDominatedRelation(prisoner, initiator);
                    Messages.Message($"[渡鸦] 肉欲让 {prisoner.LabelShort} 甘愿沦为奴隶！", prisoner, MessageTypeDefOf.PositiveEvent);
                    graphicsDirty = true;
                }
            }
            else if (ModsConfig.IdeologyActive && prisoner.guest.ExclusiveInteractionMode == PrisonerInteractionModeDefOf.Convert && prisoner.Ideo != initiator.Ideo)
            {
                prisoner.ideo.OffsetCertainty(-forceLovinCertaintyReduction);
            }

            if (!prisoner.guest.Recruitable && Rand.Chance(forceLovinBreakLoyaltyChance))
            {
                prisoner.guest.Recruitable = true;
                prisoner.guest.SetExclusiveInteraction(PrisonerInteractionModeDefOf.AttemptRecruit);
                Messages.Message($"[渡鸦] {prisoner.LabelShort} 的死忠被不可抵挡的情欲冲破了！", prisoner, MessageTypeDefOf.PositiveEvent);
                graphicsDirty = true;
            }

            if (graphicsDirty)
            {
                prisoner.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        private static void AddDominatedRelation(Pawn subject, Pawn master)
        {
            if (subject != null && master != null)
            {
                PawnRelationDef relDef = DefDatabase<PawnRelationDef>.GetNamedSilentFail("Raven_Relation_Dominated");
                if (relDef != null)
                {
                    subject.relations.AddDirectRelation(relDef, master);
                }
            }
        }
    }
}