using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.MasturbatorCup
{
    /// <summary>
    /// 使用飞机杯自慰的 JobDriver (针对持有者)
    /// </summary>
    public class JobDriver_MasturbateWithCup : JobDriver
    {
        // 延长到 2500 ticks (约 41 秒)
        private const int DurationTicks = 2500;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. 准备阶段：强制停止移动
            yield return Toils_General.StopDead();

            // 2. 执行阶段
            Toil doIt = ToilMaker.MakeToil("Masturbate");
            doIt.defaultCompleteMode = ToilCompleteMode.Delay;
            doIt.defaultDuration = DurationTicks;

            doIt.initAction = delegate
            {
                // [核心] 添加一个临时的麻醉/定身 Buff 防止玩家操作移动
                // 这里我们用一种比较温和的方式：不断击晕自己微小时间，或者直接 Pather.Stop
            };

            doIt.tickAction = delegate
            {
                // [强控] 每一帧都禁止移动
                pawn.pather.StopDead();

                // [核心修复] GainComfortFromCellIfPossible 需要 delta 参数
                pawn.GainComfortFromCellIfPossible(1);
                JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.EndJob, 1.0f, null);

                // [特效] 视觉反馈
                if (pawn.IsHashIntervalTick(60) && pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                }

                // [新增] 逐渐增加 Hediff (催情效果)
                if (pawn.IsHashIntervalTick(120) && RavenDefOf.RavenHediff_AphrodisiacEffect != null)
                {
                    HealthUtility.AdjustSeverity(pawn, RavenDefOf.RavenHediff_AphrodisiacEffect, 0.05f);
                }
            };

            doIt.AddFinishAction(delegate
            {
                if (pawn == null || pawn.Destroyed) return;

                // 结束时添加高潮 Buff
                if (RavenDefOf.Raven_Hediff_HighClimax != null)
                {
                    HealthUtility.AdjustSeverity(pawn, RavenDefOf.Raven_Hediff_HighClimax, 1.0f);
                }

                // 心情记忆
                if (RavenDefOf.Raven_Thought_MasturbatedWithCup != null)
                {
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RavenDefOf.Raven_Thought_MasturbatedWithCup);
                }
            });

            yield return doIt;
        }
    }
}