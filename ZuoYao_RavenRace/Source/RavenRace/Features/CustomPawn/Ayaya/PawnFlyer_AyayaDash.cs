using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    /// <summary>
    /// Ayaya 专属冲刺飞行器：无双风神
    /// 职责：
    /// 1. 提供平滑的飞行位移（由基类 PawnFlyer 处理）
    /// 2. 每隔数帧在当前绘制位置生成残影 Fleck，增强视觉效果
    /// 3. 落地时产生烟尘冲击特效
    /// 注：路径伤害和弹幕发射已全部在 Verb_AyayaMusouFuujin.TryCastShot 中处理
    /// 此处不再做任何战斗逻辑
    /// </summary>
    public class PawnFlyer_AyayaDash : PawnFlyer
    {
        /// <summary>
        /// 渲染重写：在绘制本体之前生成残影 Fleck
        /// 每3帧生成一个白色剪影，模拟高速移动的视觉拖尾
        /// PsycastSkipFlashEntry 是内置的淡出白色剪影，适合做残影效果
        /// </summary>
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // 每3帧生成一个残影，频率控制避免性能浪费
            if (Find.TickManager.TicksGame % 3 == 0 && this.Map != null)
            {
                FleckMaker.Static(drawLoc, this.Map, FleckDefOf.PsycastSkipFlashEntry, 0.8f);
            }
            // 调用基类绘制：阴影和携带物
            base.DrawAt(drawLoc, flip);
        }

        /// <summary>
        /// 落地回调：产生气流冲击烟尘
        /// 注意：必须在 base.RespawnPawn() 之前缓存地图和位置引用
        /// 因为基类会将 Pawn 从 innerContainer 取出，此后 this.Map 可能发生变化
        /// </summary>
        protected override void RespawnPawn()
        {
            // 在基类执行前缓存地图和位置
            Map currentMap = this.Map;
            IntVec3 landPos = this.Position;

            // 执行落地逻辑（将 Pawn 放回地图）
            base.RespawnPawn();

            // 落地烟尘特效，模拟急停产生的气浪
            if (currentMap != null)
            {
                FleckMaker.ThrowDustPuff(landPos.ToVector3Shifted(), currentMap, 2.5f);
            }
        }
    }
}