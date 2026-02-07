using Verse;
using RimWorld;

namespace RavenRace
{
    public class Hediff_AnestheticBuildup : HediffWithComps
    {
        // 严重程度随时间自然衰减（由 HediffComp_SeverityPerDay 控制）
        // 增加逻辑由气体触发

        public override void Tick()
        {
            base.Tick();

            // 如果严重程度达到 1.0，触发昏迷
            if (this.Severity >= 1.0f && !pawn.health.hediffSet.HasHediff(HediffDefOf.Anesthetic))
            {
                pawn.health.AddHediff(HediffDefOf.Anesthetic);
                Messages.Message($"{pawn.LabelShort} 被麻醉气体击倒了！", pawn, MessageTypeDefOf.NeutralEvent);

                // 昏迷后清空累积值，防止反复触发消息
                this.Severity = 0.1f;
            }
        }
    }
}