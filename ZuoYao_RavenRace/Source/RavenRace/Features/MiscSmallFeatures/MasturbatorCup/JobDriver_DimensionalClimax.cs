using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.MasturbatorCup
{
    /// <summary>
    /// 次元性交受害者的 JobDriver
    /// </summary>
    public class JobDriver_DimensionalClimax : JobDriver
    {
        private Pawn Caster => job?.targetA.Pawn;
        private const int DurationTicks = 2500;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. 强制倒地/停止
            yield return Toils_General.StopDead();

            Toil suffer = ToilMaker.MakeToil("SufferPleasure");
            suffer.defaultCompleteMode = ToilCompleteMode.Delay;
            suffer.defaultDuration = DurationTicks;

            suffer.initAction = delegate
            {
                if (pawn.Map != null)
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "❤~!", Color.magenta);
                }

                if (pawn.stances != null && pawn.stances.stunner != null)
                {
                    pawn.stances.stunner.StunFor(DurationTicks, Caster, false, true);
                }
            };

            suffer.tickAction = delegate
            {
                // [兼容性修复] 如果 Pawn 已经倒地或失去意识，优雅地结束任务
                // 不要继续执行 Tick 逻辑，防止与底层 Downed 处理冲突
                if (pawn.Downed || pawn.Dead || !pawn.Spawned)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                if (pawn.pather != null) pawn.pather.StopDead();

                if (pawn.IsHashIntervalTick(60) && pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                }

                if (pawn.IsHashIntervalTick(100) && RavenDefOf.RavenHediff_AphrodisiacEffect != null)
                {
                    // 加 Buff 可能会导致意识归零 -> 倒地 -> 触发 Job 结束
                    // 只要我们的补丁Patch_AddHediff_ClearAggro放行了，这里就安全了
                    HealthUtility.AdjustSeverity(pawn, RavenDefOf.RavenHediff_AphrodisiacEffect, 0.05f);
                }

                if (pawn.needs != null && pawn.needs.joy != null && RavenDefOf.Raven_AdultEntertainment != null)
                {
                    pawn.needs.joy.GainJoy(0.005f, RavenDefOf.Raven_AdultEntertainment);
                }
            };

            suffer.AddFinishAction(delegate
            {
                // [安全检查] 必须检查 pawn 状态
                if (pawn == null || pawn.Destroyed) return;

                Pawn casterPawn = Caster;

                if (casterPawn != null && RavenDefOf.Raven_Thought_ForceLovin_Recipient != null)
                {
                    if (pawn.needs != null && pawn.needs.mood != null && pawn.needs.mood.thoughts != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_ForceLovin_Recipient, casterPawn);
                    }
                }

                if (RavenDefOf.Raven_Hediff_HighClimax != null)
                {
                    HealthUtility.AdjustSeverity(pawn, RavenDefOf.Raven_Hediff_HighClimax, 1.0f);

                    // 如果还没倒地，补一刀
                    if (!pawn.Downed && pawn.stances != null && pawn.stances.stunner != null)
                    {
                        pawn.stances.stunner.StunFor(1200, casterPawn, true, true);
                    }

                    // 仅当状态改变时发消息，防止刷屏
                    Messages.Message($"{pawn.LabelShort} 在次元刺激下彻底失去了意识。", pawn, MessageTypeDefOf.NegativeEvent);
                }
            });

            yield return suffer;
        }
    }
}