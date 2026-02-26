using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    /// <summary>
    /// Ayaya 专属冲刺飞行器：无双风神
    /// 逻辑：
    /// 1. 继承原版 PawnFlyer 实现平滑位移。
    /// 2. 在 Tick 中实时检测当前位置的敌人，造成穿透伤害。
    /// 3. 绘制时生成残影特效。
    /// </summary>
    public class PawnFlyer_AyayaDash : PawnFlyer
    {
        // 冲刺造成的伤害值
        private static readonly int DashDamage = 25;
        // 记录已命中的敌人ID，防止单次冲刺对同一目标造成多次判定 (HashSet查找效率O(1))
        private HashSet<int> hitTargets = new HashSet<int>();

        /// <summary>
        /// 每帧更新逻辑 (Tick)
        /// 负责驱动飞行并执行碰撞检测
        /// </summary>
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            // 只有在飞行中且地图存在时才检测，避免空引用
            if (this.FlyingPawn != null && this.Map != null)
            {
                CheckCollision();
            }
        }

        /// <summary>
        /// 碰撞检测逻辑
        /// 检测飞行器当前坐标格内的所有物体，对敌人造成伤害
        /// </summary>
        private void CheckCollision()
        {
            // 获取飞行器当前的绘制位置 (精确坐标)
            Vector3 currentPos = this.DrawPos;
            IntVec3 currentCell = currentPos.ToIntVec3();

            // 检测当前格子内的所有物体
            // 注意：使用 ThingGrid 来获取列表比遍历全图效率高得多
            List<Thing> thingList = this.Map.thingGrid.ThingsListAt(currentCell);

            // 倒序遍历以防在处理过程中集合被修改 (虽然此处主要是造成伤害)
            for (int i = thingList.Count - 1; i >= 0; i--)
            {
                Thing t = thingList[i];
                // 1. 是 Pawn
                // 2. 不是自己
                // 3. 还没被这次冲刺打过
                if (t is Pawn victim && victim != this.FlyingPawn && !hitTargets.Contains(victim.thingIDNumber))
                {
                    // 敌对判定：只伤害敌人，不误伤友军
                    if (victim.HostileTo(this.FlyingPawn))
                    {
                        DoDashDamage(victim);
                        hitTargets.Add(victim.thingIDNumber);
                    }
                }
            }
        }

        /// <summary>
        /// 执行冲刺伤害逻辑
        /// </summary>
        /// <param name="victim">受害者</param>
        private void DoDashDamage(Pawn victim)
        {
            // 构造伤害信息 (Cut - 切割伤害)
            // 护甲穿透设为 0.5f，确保对重甲单位也有一定效果
            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.Cut,
                DashDamage,
                0.5f,
                -1,
                this.FlyingPawn,
                null,
                null
            );

            victim.TakeDamage(dinfo);

            // 视觉反馈：微量火花表示切割命中，提升打击感
            FleckMaker.ThrowMicroSparks(victim.DrawPos, this.Map);

            // 播放切割音效
            // [修复] 使用自定义 DefOf 引用，确保音效存在，避免 SoundDefOf 缺失导致的编译错误
            if (AyayaDefOf.Raven_Sound_Ayaya_Slash != null)
            {
                AyayaDefOf.Raven_Sound_Ayaya_Slash.PlayOneShot(new TargetInfo(victim.Position, this.Map));
            }
        }

        /// <summary>
        /// 渲染重写：添加残影
        /// 必须使用 protected override 以匹配原版 Thing.DrawAt 的签名
        /// </summary>
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // 1. 每隔 3 Tick 在当前位置生成一个静态残影 Fleck
            // PsycastSkipFlashEntry 是一个淡出的白色剪影，非常适合做高速移动的残影
            if (Find.TickManager.TicksGame % 3 == 0)
            {
                FleckMaker.Static(drawLoc, Map, FleckDefOf.PsycastSkipFlashEntry, 0.8f);
            }

            // 2. 调用基类绘制 Ayaya 本体的模型
            base.DrawAt(drawLoc, flip);
        }

        /// <summary>
        /// 落地回调
        /// 当飞行结束，Pawn 重新生成在地图上时调用
        /// </summary>
        protected override void RespawnPawn()
        {
            base.RespawnPawn();

            // 落地时产生一圈无伤害的烟雾冲击波，增加视觉力度，表现“急停”的气势
            if (this.FlyingPawn != null && this.Map != null)
            {
                FleckMaker.ThrowDustPuff(this.Position, this.Map, 2.0f);
            }
        }
    }
}