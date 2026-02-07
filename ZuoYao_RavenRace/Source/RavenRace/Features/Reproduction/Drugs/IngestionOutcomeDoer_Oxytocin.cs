using System;
using Verse;
using RimWorld;
using RavenRace.Features.Reproduction; // 引用自身命名空间下的 Hediff

namespace RavenRace.Features.Reproduction
{
    /// <summary>
    /// 催产素服用效果：
    /// 强制给 Pawn 添加 "强制排卵" (Rapid Ovulation) 状态。
    /// </summary>
    public class IngestionOutcomeDoer_Oxytocin : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            // 只有渡鸦族或（开启设置后的）男性可以生效
            bool isRaven = pawn.def == RavenDefOf.Raven_Race;
            bool malePregnancy = RavenRaceMod.Settings.enableMalePregnancyEgg;
            bool validSubject = isRaven || (malePregnancy && pawn.gender == Gender.Male);

            if (validSubject)
            {
                // 创建并添加 Hediff
                Hediff hediff = HediffMaker.MakeHediff(RavenDefOf.Raven_Hediff_RapidOvulation, pawn);
                hediff.Severity = 0.1f;
                pawn.health.AddHediff(hediff);

                // 造成短暂晕眩
                pawn.stances.stunner.StunFor(120, null, true, true);

                Messages.Message("RavenRace_Msg_OxytocinReaction".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message("RavenRace_Msg_OxytocinNoEffect".Translate(), pawn, MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}