using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.Reproduction
{
    /// <summary>
    /// 全局定时器：实时同步拥有“繁衍至上”模因的小人的交配次数与其增益状态。
    /// 完全解耦了心情与状态的强制绑定。
    /// </summary>
    public class GameComponent_ReproductionHediffSync : GameComponent
    {
        public GameComponent_ReproductionHediffSync(Game game)
        {
        }

        public override void GameComponentTick()
        {
            // 性能优化：每 2500 ticks (游戏内1小时) 执行一次状态刷新
            if (Find.TickManager.TicksGame % 2500 != 0) return;

            // 获取玩家派系所有活着的人类
            List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction;
            if (pawns == null) return;

            foreach (Pawn pawn in pawns)
            {
                // 如果角色拥有文化信仰，且信仰中包含我们的“繁衍至上”模因
                if (pawn.Ideo != null && pawn.Ideo.HasMeme(RavenDefOf.Raven_Heritage))
                {
                    UpdateHediff(pawn);
                }
                else
                {
                    // 如果因为文化转变失去了该模因，则清除其状态
                    RemoveHediff(pawn);
                }
            }
        }

        private void UpdateHediff(Pawn pawn)
        {
            if (pawn.records == null) return;

            int lovinCount = pawn.records.GetAsInt(RavenDefOf.Raven_Record_LovinCount);

            // 基于次数判断处于哪个阶段 (Severity 值取该 Stage 的中间值以确保稳定)
            float targetSeverity = 0.5f; // Stage 0 (0-9次)

            if (lovinCount >= 200) targetSeverity = 3.5f;      // Stage 3
            else if (lovinCount >= 50) targetSeverity = 2.5f;  // Stage 2
            else if (lovinCount >= 10) targetSeverity = 1.5f;  // Stage 1

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(RavenDefOf.Raven_Hediff_ReproductionLust);

            if (hediff == null)
            {
                hediff = pawn.health.AddHediff(RavenDefOf.Raven_Hediff_ReproductionLust);
            }

            // 只有当严重度不一致时才修改，避免触发不必要的严重度变更回调和 UI 刷新
            if (Mathf.Abs(hediff.Severity - targetSeverity) > 0.01f)
            {
                hediff.Severity = targetSeverity;
            }
        }

        private void RemoveHediff(Pawn pawn)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(RavenDefOf.Raven_Hediff_ReproductionLust);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
}