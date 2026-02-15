using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    /// <summary>
    /// 仅当 Pawn 拥有特定 Hediff 时才渲染此节点。
    /// XML 中需要在 <pawnRenderNode> 下配置 <workerClass>。
    /// </summary>
    public class PawnRenderNodeWorker_HediffVisual : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            // 基础检查
            if (!base.CanDrawNow(node, parms)) return false;

            // 检查 Hediff
            // 这里我们硬编码检查劣化之锁，或者你可以扩展 PawnRenderNodeProperties 来传入 DefName (更复杂)
            // 为了简单直接，我们检查 BinahDefOf.Raven_Hediff_Binah_DegradationLock

            if (parms.pawn.health == null || parms.pawn.health.hediffSet == null) return false;

            if (parms.pawn.health.hediffSet.HasHediff(BinahDefOf.Raven_Hediff_Binah_DegradationLock))
            {
                return true;
            }

            return false;
        }

        // 确保它覆盖在身体之上
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 offset = base.OffsetFor(node, parms, out pivot);
            // 稍微抬高 Y 轴，确保覆盖在衣服上
            offset.y += 0.02f;
            return offset;
        }
    }
}