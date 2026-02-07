using Verse;
using RimWorld;
using UnityEngine;
using RavenRace.Features.Bloodline;

namespace RavenRace
{
    public class IngestionOutcomeDoer_EmberBlood : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            float deathChance = RavenRaceMod.Settings.emberBloodDeathChance;
            float berserkChance = RavenRaceMod.Settings.emberBloodBerserkChance;
            float roll = Rand.Value;

            Map map = pawn.Map;
            Vector3 drawPos = pawn.DrawPos;

            if (roll < deathChance)
            {
                Find.LetterStack.ReceiveLetter(
                    "注射失败：死亡",
                    $"{pawn.LabelShort} 无法承受余烬之血的力量，血管爆裂而亡！",
                    LetterDefOf.Death,
                    new TargetInfo(pawn.Position, map)
                );
                if (map != null) MoteMaker.ThrowText(drawPos, map, "☠", 5f);
                DamageInfo dinfo = new DamageInfo(DamageDefOf.ExecutionCut, 99999f, 999f, -1f, null, pawn.RaceProps.body.corePart);
                pawn.Kill(dinfo);
            }
            else if (roll < deathChance + berserkChance)
            {
                Messages.Message($"{pawn.LabelShort} 被狂暴的力量吞噬了理智！", pawn, MessageTypeDefOf.NegativeEvent);
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "余烬侵蚀", true);
            }
            else
            {
                var comp = pawn.TryGetComp<CompBloodline>();
                if (comp != null)
                {
                    float gain = 0.1f;
                    comp.GoldenCrowConcentration += gain;
                    Messages.Message($"{pawn.LabelShort} 成功吸收了余烬之血，金乌血脉得到了纯化 (+{gain:P0})。", pawn, MessageTypeDefOf.PositiveEvent);

                    comp.RefreshAbilities();
                    pawn.Drawer?.renderer?.SetAllGraphicsDirty();
                }
                else
                {
                    Messages.Message($"{pawn.LabelShort} 没有任何反应 (非血脉持有者)。", pawn, MessageTypeDefOf.NeutralEvent);
                }
            }
        }
    }
}