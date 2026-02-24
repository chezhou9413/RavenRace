using HarmonyLib;
using Verse;
using UnityEngine;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.Bathtub
{
    /// <summary>
    /// 浴缸渲染核心补丁：负责脱衣、精准偏移以及躺下时的旋转
    /// </summary>
    public static class Patch_BathtubRendering
    {
        // 偏移量：正数表示向浴缸的朝向（圆弧面/头部）滑动。你可以根据实际贴图效果微调这个值。
        private const float HeadOffset = 0.45f;

        // 1. 核心渲染拦截：脱去衣物，并将小人的坐标强行偏移到浴缸正确位置
        [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
        public static class Patch_GetDrawParms
        {
            public static void Postfix(Pawn ___pawn, ref PawnDrawParms __result)
            {
                // 【核心逻辑闭环】必须同时满足：1. 执行洗澡任务；2. 姿态是“躺下”。
                // 这样小人在寻路走过去的时候绝对不会触发吸附和脱衣！
                if (___pawn != null &&
                    ___pawn.CurJobDef == RavenDefOf.Raven_Job_TakeSlimeBath &&
                    ___pawn.CurJob.targetA.Thing != null &&
                    ___pawn.GetPosture() == PawnPosture.LayingOnGroundFaceUp)
                {
                    // A. 隐藏衣物和帽子
                    __result.flags &= ~PawnRenderFlags.Clothes;
                    __result.flags &= ~PawnRenderFlags.Headgear;

                    // B. 计算坐标偏移
                    Thing bathtub = ___pawn.CurJob.targetA.Thing;

                    // 获取原版计算出的 Y 轴高度（确保不被地板遮挡，且被水面覆盖）
                    float currentY = __result.matrix.m13;

                    // 获取浴缸的绝对几何中心
                    Vector3 basePos = bathtub.DrawPos;

                    // 计算沿着浴缸朝向（圆弧面方向）的矢量位移
                    Vector3 offset = bathtub.Rotation.FacingCell.ToVector3() * HeadOffset;

                    // 最终渲染坐标 = 中心点 + 矢量位移
                    Vector3 finalPos = new Vector3(basePos.x + offset.x, currentY, basePos.z + offset.z);

                    // 重新构建渲染矩阵，方向使用浴缸方向，缩放保持为1
                    __result.matrix = Matrix4x4.TRS(
                        finalPos,
                        Quaternion.AngleAxis(bathtub.Rotation.AsAngle, Vector3.up),
                        Vector3.one
                    );
                }
            }
        }

        // 2. 修正朝向：头部靠着圆弧面
        [HarmonyPatch(typeof(PawnRenderer), "BodyAngle")]
        public static class Patch_BodyAngle
        {
            public static void Postfix(Pawn ___pawn, ref float __result)
            {
                // 同样加入姿态锁，确保走路时朝向正常
                if (___pawn != null &&
                    ___pawn.CurJobDef == RavenDefOf.Raven_Job_TakeSlimeBath &&
                    ___pawn.CurJob.targetA.Thing != null &&
                    ___pawn.GetPosture() == PawnPosture.LayingOnGroundFaceUp)
                {
                    // 直接使用浴缸的角度
                    __result = ___pawn.CurJob.targetA.Thing.Rotation.AsAngle;
                }
            }
        }
    }
}