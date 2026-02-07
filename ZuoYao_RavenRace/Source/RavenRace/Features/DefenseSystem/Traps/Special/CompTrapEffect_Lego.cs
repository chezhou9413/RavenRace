using Verse;
using RimWorld;
using Verse.Sound;

namespace RavenRace
{
    public class CompTrapEffect_Lego : CompTrapEffect
    {
        public override void OnTriggered(Pawn triggerer)
        {
            if (triggerer == null) return;

            // [Fixed] 用户反馈声音太吵，已禁用
            // SoundDefOf.Crunch.PlayOneShot(new TargetInfo(parent.Position, parent.Map));

            // 2. 添加极度疼痛 Hediff
            Hediff pain = HediffMaker.MakeHediff(DefenseDefOf.RavenHediff_LegoPain, triggerer);
            triggerer.health.AddHediff(pain);

            // 3. 强制倒地 (Stun)
            // 痛得跳脚2秒 (120 ticks)
            if (triggerer.stances != null && triggerer.stances.stunner != null)
            {
                triggerer.stances.stunner.StunFor(120, parent, true, true);
            }

            // 4. 发送消息
            Messages.Message($"{triggerer.LabelShort} 踩到了乐高！这真是太残忍了！", triggerer, MessageTypeDefOf.NegativeEvent);

            // 乐高不销毁 (太硬了)
        }
    }
}