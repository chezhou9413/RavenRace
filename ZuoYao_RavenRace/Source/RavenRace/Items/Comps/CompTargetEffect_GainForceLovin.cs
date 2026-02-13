using Verse;
using RimWorld;

namespace RavenRace.Items.Comps
{
    /// <summary>
    /// 使用物品后赋予“强制求爱”技能的效果组件。
    /// </summary>
    public class CompTargetEffect_GainForceLovin : CompTargetEffect
    {
        /// <summary>
        /// 对目标执行效果。
        /// </summary>
        /// <param name="user">使用者 (这里不重要)</param>
        /// <param name="target">被选择的目标 Pawn</param>
        public override void DoEffectOn(Pawn user, Thing target)
        {
            // 确保目标是 Pawn
            if (target is Pawn targetPawn)
            {
                // 检查 Pawn 是否有能力组件
                if (targetPawn.abilities == null)
                {
                    Messages.Message($"{targetPawn.LabelShortCap} 无法学习任何技能。", MessageTypeDefOf.RejectInput);
                    return;
                }

                // 检查是否已经有该技能
                if (targetPawn.abilities.GetAbility(RavenDefOf.Raven_Ability_ForceLovin) != null)
                {
                    Messages.Message($"{targetPawn.LabelShortCap} 已经掌握了这项技能。", MessageTypeDefOf.RejectInput);
                    return;
                }

                // 赋予技能
                targetPawn.abilities.GainAbility(RavenDefOf.Raven_Ability_ForceLovin);

                // 显示反馈信息
                Messages.Message($"{targetPawn.LabelShortCap} 学习并掌握了“强制求爱”技能！", targetPawn, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}