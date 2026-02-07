using RimWorld;
using Verse;
using System.Linq;
using System.Text;
using RavenRace.Features.BedSharing;

namespace RavenRace.Features.DegradationCharm.Hediffs
{
    public class Hediff_Degradation : HediffWithComps
    {
        private bool isTransforming = false;

        public override string TipStringExtra
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(base.TipStringExtra);
                if (Severity >= 0.75f)
                {
                    sb.AppendLine("\n理智的枷锁已被彻底粉碎。大脑一片空白，只剩下被填满、被占有、被当作玩物的本能。身体变成了纯粹的欲望容器，向每一个可能的伴侣散发着毫不掩饰的邀请。“求求你……对我做什么都可以……只要让我高潮……”");
                }
                else if (Severity >= 0.50f)
                {
                    sb.AppendLine("\n身体的欲望已经开始主导思维，对欢愉的渴求变得难以抑制。羞耻感？那是什么？能吃吗？现在，思考的全部意义，就是如何诱惑下一个人，将自己完全奉献出去，在交合的汗水中感受存在的意义。");
                }
                else if (Severity >= 0.25f)
                {
                    sb.AppendLine("\n一股难以言喻的燥热感从小腹升起，流遍四肢百骸。那些羞耻的幻想也越来越频繁，甚至在与人交谈时，目光都会不自觉地滑向对方的身体。理智的防线似乎正在被快感的水位一点点淹没。");
                }
                else
                {
                    sb.AppendLine("\n额头上的符咒传来微弱的热量，身体变得有些敏感。偶尔会有些奇怪的念头闪过脑海，但很快就被羞耻心压了下去。“我……我才不会想那种事！”");
                }
                sb.AppendLine("\n" + "堕落值".Colorize(ColoredText.TipSectionTitleColor) + ": " + Severity.ToStringPercent());
                return sb.ToString();
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (!isTransforming && Severity >= 1.0f)
            {
                isTransforming = true;
                TransformPawn();
            }
        }

        private void TransformPawn()
        {
            Find.LetterStack.ReceiveLetter(
                "Raven_LetterLabel_CharmComplete".Translate(pawn.LabelShort),
                "Raven_LetterText_CharmComplete".Translate(pawn.Named("PAWN")),
                LetterDefOf.PositiveEvent,
                pawn
            );

            Trait asexualTrait = pawn.story.traits.GetTrait(TraitDefOf.Asexual);
            if (asexualTrait != null)
            {
                pawn.story.traits.RemoveTrait(asexualTrait);
            }

            if (!pawn.story.traits.HasTrait(TraitDefOf.Bisexual))
            {
                pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual));
            }

            if (!pawn.story.traits.HasTrait(DegradationCharmDefOf.Raven_Trait_Lecherous))
            {
                pawn.story.traits.GainTrait(new Trait(DegradationCharmDefOf.Raven_Trait_Lecherous));
            }

            if (ModsConfig.IdeologyActive && pawn.Ideo != null)
            {
                pawn.ideo.OffsetCertainty(-1f);
            }

            if (pawn.gender == Gender.Male)
            {
                pawn.gender = Gender.Female;
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }

            pawn.health.RemoveHediff(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isTransforming, "isTransforming", false);
        }
    }
}