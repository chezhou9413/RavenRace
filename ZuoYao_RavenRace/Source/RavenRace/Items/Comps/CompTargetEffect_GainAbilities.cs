using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Items.Comps
{
    /// <summary>
    /// 通用技能学习组件属性。
    /// 允许在 XML 中配置一个技能列表，使用后一次性全部赋予。
    /// </summary>
    public class CompProperties_GainAbilities : CompProperties_UseEffect
    {
        public List<AbilityDef> abilityDefs = new List<AbilityDef>();

        public CompProperties_GainAbilities()
        {
            this.compClass = typeof(CompTargetEffect_GainAbilities);
        }
    }

    /// <summary>
    /// 使用物品后赋予指定列表内所有技能的效果组件。
    /// </summary>
    public class CompTargetEffect_GainAbilities : CompTargetEffect
    {
        public CompProperties_GainAbilities Props => (CompProperties_GainAbilities)props;

        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is Pawn targetPawn)
            {
                if (targetPawn.abilities == null)
                {
                    Messages.Message($"{targetPawn.LabelShortCap} 的心智无法承受这种禁忌的知识。", MessageTypeDefOf.RejectInput);
                    return;
                }

                if (Props.abilityDefs == null || Props.abilityDefs.Count == 0) return;

                bool learnedAny = false;

                foreach (AbilityDef abilityDef in Props.abilityDefs)
                {
                    if (targetPawn.abilities.GetAbility(abilityDef) == null)
                    {
                        targetPawn.abilities.GainAbility(abilityDef);
                        learnedAny = true;
                    }
                }

                if (learnedAny)
                {
                    Messages.Message($"{targetPawn.LabelShortCap} 脑海中涌入了大量淫靡的画面，身体彻底觉醒了渡鸦的深层本能！", targetPawn, MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message($"{targetPawn.LabelShortCap} 早就对这些淫乱的伎俩了如指掌了。", MessageTypeDefOf.RejectInput);
                }
            }
        }
    }
}