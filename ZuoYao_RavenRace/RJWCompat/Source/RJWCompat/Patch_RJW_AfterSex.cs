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
    /// </summary>
    [HarmonyPatch(typeof(SexUtility), "Aftersex")]
    public static class Patch_RJW_AfterSex
    {
        [HarmonyPostfix]
        public static void Postfix(SexProps props)
        {
            // 安全检查
            if (props == null || props.pawn == null || props.partner == null) return;

            Pawn initiator = props.pawn; // 发起者 (渡鸦)
            Pawn partner = props.partner; // 承受者 (可能是囚犯)

            // ------------------------------------------------------
            // 1. 处理“淫堕刻印” (Degradation Charm) 增加逻辑
            // ------------------------------------------------------
            IncreaseDegradationIfPresent(initiator);
            IncreaseDegradationIfPresent(partner);

            // ------------------------------------------------------
            // 2. 处理“强制求爱”带来的囚犯互动 (降低抵抗/意志)
            // ------------------------------------------------------
            // 条件：发起者是渡鸦族 (或拥有该能力)，且对象是本殖民地的囚犯
            // 注意：因为我们无法轻易区分这是 "ForceLovin" 还是普通的 RJW 性爱，
            // 这里我们假设只要是渡鸦族发起的对囚犯的性行为，都视作这种互动。
            if (initiator.def == RavenDefOf.Raven_Race && partner.IsPrisonerOfColony)
            {
                HandleRavenPrisonerInteraction(initiator, partner);
            }
        }

        /// <summary>
        /// 增加淫堕 Hediff 的严重度
        /// </summary>
        private static void IncreaseDegradationIfPresent(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;

            // 使用 DefOf 获取 HediffDef
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DegradationCharmDefOf.Raven_Hediff_Degradation);
            if (hediff != null)
            {
                // 每次 RJW 性行为增加 0.1
                hediff.Severity += 0.10f;
            }
        }

        /// <summary>
        /// 处理囚犯互动逻辑 (从 JobDriver_ForceLovin 移植并适配)
        /// </summary>
        private static void HandleRavenPrisonerInteraction(Pawn initiator, Pawn prisoner)
        {
            var s = RavenRaceMod.Settings;
            if (s == null) return;
            bool graphicsDirty = false;

            // A. 招募模式 -> 降抵抗
            if (prisoner.guest.ExclusiveInteractionMode == PrisonerInteractionModeDefOf.AttemptRecruit && prisoner.guest.resistance > 0)
            {
                float reduction = s.forceLovinResistanceReduction;
                prisoner.guest.resistance = Mathf.Max(0, prisoner.guest.resistance - reduction);
                Messages.Message("RavenRace_Msg_ResistanceLowered".Translate(prisoner.LabelShort, reduction), prisoner, MessageTypeDefOf.PositiveEvent);

                // 瞬间招募判定
                if (prisoner.guest.resistance <= 0 && Rand.Chance(s.forceLovinInstantRecruitChance))
                {
                    InteractionWorker_RecruitAttempt.DoRecruit(initiator, prisoner);
                    AddDominatedRelation(prisoner, initiator);
                    Messages.Message("RavenRace_Msg_InstantRecruit".Translate(prisoner.LabelShort), prisoner, MessageTypeDefOf.PositiveEvent);
                    graphicsDirty = true;
                }
            }
            // B. 奴役模式 -> 降意志
            else if (prisoner.guest.ExclusiveInteractionMode == PrisonerInteractionModeDefOf.Enslave && prisoner.guest.will > 0)
            {
                float reduction = s.forceLovinWillReduction;
                prisoner.guest.will = Mathf.Max(0, prisoner.guest.will - reduction);
                Messages.Message("RavenRace_Msg_WillLowered".Translate(prisoner.LabelShort, reduction), prisoner, MessageTypeDefOf.PositiveEvent);

                // 瞬间奴役判定
                if (prisoner.guest.will <= 0 && Rand.Chance(s.forceLovinInstantRecruitChance))
                {
                    GenGuest.EnslavePrisoner(initiator, prisoner);
                    AddDominatedRelation(prisoner, initiator);
                    Messages.Message("RavenRace_Msg_InstantEnslave".Translate(prisoner.LabelShort), prisoner, MessageTypeDefOf.PositiveEvent);
                    graphicsDirty = true;
                }
            }
            // C. 转化模式 -> 降信仰
            else if (ModsConfig.IdeologyActive && prisoner.guest.ExclusiveInteractionMode == PrisonerInteractionModeDefOf.Convert && prisoner.Ideo != initiator.Ideo)
            {
                prisoner.ideo.OffsetCertainty(-s.forceLovinCertaintyReduction);
            }

            // D. 打破死忠
            if (!prisoner.guest.Recruitable && Rand.Chance(s.forceLovinBreakLoyaltyChance))
            {
                prisoner.guest.Recruitable = true;
                prisoner.guest.SetExclusiveInteraction(PrisonerInteractionModeDefOf.AttemptRecruit);
                Messages.Message("RavenRace_Msg_LoyaltyBroken".Translate(prisoner.LabelShort), prisoner, MessageTypeDefOf.PositiveEvent);
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
                subject.relations.AddDirectRelation(RavenDefOf.Raven_Relation_Dominated, master);
            }
        }
    }
}