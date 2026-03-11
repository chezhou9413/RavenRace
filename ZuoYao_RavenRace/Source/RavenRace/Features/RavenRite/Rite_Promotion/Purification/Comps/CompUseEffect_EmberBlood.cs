using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.RavenRite.Rite_Promotion.Purification.Comps
{
    /// <summary>
    /// 余烬之血的全新执行逻辑。
    /// 修复了1.6版本中 CanBeUsedBy 返回 AcceptanceReport 的问题。
    /// </summary>
    public class CompUseEffect_EmberBlood : CompUseEffect
    {
        private const float EmberBloodMaxLimit = 0.20f;
        private const float EmberBloodGain = 0.02f;

        /// <summary>
        /// 1.6 版本原生拦截机制。如果返回非Accepted，右键菜单将变灰并显示原因。
        /// </summary>
        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            // 只有装配了金乌纯化组件的种族才能用
            var comp = p.TryGetComp<CompPurification>();

            if (comp == null)
            {
                return new AcceptanceReport("体内没有一丝金乌的血脉响应。");
            }

            // 浓度已经达到或超过了余烬之血能提升的极限
            if (comp.GoldenCrowConcentration >= EmberBloodMaxLimit)
            {
                return new AcceptanceReport($"金乌浓度已达到或超过 {EmberBloodMaxLimit:P0}，该低级媒介无法再引起共鸣。");
            }

            return base.CanBeUsedBy(p);
        }

        /// <summary>
        /// 使用后执行的具体效果
        /// </summary>
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            float deathChance = RavenRaceMod.Settings.emberBloodDeathChance;
            float berserkChance = RavenRaceMod.Settings.emberBloodBerserkChance;
            float roll = Rand.Value;

            Map map = usedBy.Map;
            Vector3 drawPos = usedBy.DrawPos;

            if (roll < deathChance)
            {
                Find.LetterStack.ReceiveLetter(
                    "RavenRace_LetterLabel_EmberFailedDeath".Translate(), // "注射失败：死亡"
                    "RavenRace_LetterText_EmberFailedDeath".Translate(usedBy.LabelShort),
                    LetterDefOf.Death,
                    new TargetInfo(usedBy.Position, map)
                );
                if (map != null) MoteMaker.ThrowText(drawPos, map, "☠", 5f);
                DamageInfo dinfo = new DamageInfo(DamageDefOf.ExecutionCut, 99999f, 999f, -1f, null, usedBy.RaceProps.body.corePart);
                usedBy.Kill(dinfo);
            }
            else if (roll < deathChance + berserkChance)
            {
                Messages.Message("RavenRace_Msg_EmberFailedBerserk".Translate(usedBy.LabelShort), usedBy, MessageTypeDefOf.NegativeEvent);
                usedBy.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "余烬侵蚀", true);
            }
            else
            {
                var comp = usedBy.TryGetComp<CompPurification>();
                if (comp != null)
                {
                    comp.TryAddGoldenCrowConcentration(EmberBloodGain, EmberBloodMaxLimit);

                    Messages.Message($"奇迹发生！{usedBy.LabelShort} 成功压制了狂暴的力量，金乌血脉浓度微弱地提升了 (+{EmberBloodGain:P0})。", usedBy, MessageTypeDefOf.PositiveEvent);

                    FleckMaker.ThrowLightningGlow(usedBy.TrueCenter(), usedBy.Map, 1.5f);
                }
            }
        }
    }
}