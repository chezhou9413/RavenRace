using RimWorld;
using Verse;
using UnityEngine;

namespace RavenRace.Features.Hypnosis
{
    /// <summary>
    /// 物品效果组件：使用催眠App绑定目标。
    /// </summary>
    public class CompTargetEffect_HypnosisBind : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            Pawn targetPawn = target as Pawn;
            if (targetPawn == null) return;

            // 1. 注册绑定关系
            WorldComponent_Hypnosis.Instance.AddBond(user, targetPawn);

            // 2. 视觉特效
            FleckMaker.ThrowMetaIcon(targetPawn.Position, targetPawn.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.ThrowMetaIcon(user.Position, user.Map, FleckDefOf.PsycastAreaEffect);

            // 3. 消息提示
            Messages.Message("Raven_Hypnosis_BindSuccess".Translate(user.LabelShort, targetPawn.LabelShort),
                new LookTargets(user, targetPawn), MessageTypeDefOf.PositiveEvent);

            // 4. RimTalk 互动 (可选)
            RimTalkCompat.TryAddTalkRequest(user, "Connectivity verified. Target locked. Awaiting instructions.");
        }
    }
}