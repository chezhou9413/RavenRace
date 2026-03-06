using Verse;
using RimWorld;

namespace RavenRace.Features.MechanicalAngel
{
    /// <summary>
    /// 艾吉斯专属判断工具：是否正在进行榨汁充能。
    /// </summary>
    public static class AegisRenderUtility
    {
        public static bool IsChargingLust(Pawn pawn)
        {
            if (pawn == null) return false;
            JobDef chargeJob = DefDatabase<JobDef>.GetNamedSilentFail("Raven_Job_AegisLustCharge");
            return chargeJob != null && pawn.CurJobDef == chargeJob;
        }
    }

    /// <summary>
    /// 正常眼神节点控制器：仅在非榨汁状态下显示。
    /// 完美符合 1.6 渲染树规范。
    /// </summary>
    public class PawnRenderNodeWorker_AegisEyes_Normal : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            // 如果基类不允许绘制（比如头被砍了），或者正在发情榨汁，则不绘制正常眼
            if (!base.CanDrawNow(node, parms)) return false;
            return !AegisRenderUtility.IsChargingLust(parms.pawn);
        }
    }

    /// <summary>
    /// 粉色爱心眼节点控制器：仅在榨汁状态下显示。
    /// </summary>
    public class PawnRenderNodeWorker_AegisEyes_Pink : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            // 仅在执行榨汁充能工作时绘制粉色爱心眼
            if (!base.CanDrawNow(node, parms)) return false;
            return AegisRenderUtility.IsChargingLust(parms.pawn);
        }
    }
}