using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    /// <summary>
    /// 击退专用飞行器（天狗颪）
    /// 落地时对内部的 Pawn 施加钝器伤害和眩晕，模拟被强风卷起后重摔落地的效果
    /// </summary>
    public class PawnFlyer_Knockback : PawnFlyer
    {
        // 落地钝器伤害值
        private const int ImpactDamage = 20;
        // 眩晕时长（Tick），180 tick ≈ 3秒
        private const int StunDuration = 180;

        protected override void RespawnPawn()
        {
            // 必须在 base.RespawnPawn() 之前获取引用
            // 因为基类会将 Pawn 从 innerContainer 取出并重新 Spawn 到地图上
            Pawn p = this.FlyingPawn;

            // 执行落地逻辑（将 Pawn 放回地图，应用 stunDuration，etc.）
            base.RespawnPawn();

            // 落地后施加额外效果（此时 p 已经重新 Spawned 在地图上）
            if (p != null && !p.Dead && p.Spawned)
            {
                // 1. 施加落地钝器伤害（模拟重摔地面）
                DamageInfo crashDmg = new DamageInfo(
                    DamageDefOf.Blunt,
                    ImpactDamage,
                    0.1f,   // 低护甲穿透，模拟撞击地面而非锋利武器
                    -1f,
                    null,
                    null,
                    null
                );
                p.TakeDamage(crashDmg);

                // 2. 施加眩晕效果（addBattleLog=false 避免日志刷屏）
                p.stances.stunner.StunFor(StunDuration, null, false, true, false);

                // 3. 落地烟尘特效
                if (p.Map != null)
                {
                    FleckMaker.ThrowDustPuff(p.Position.ToVector3Shifted(), p.Map, 2.5f);
                }
            }
        }
    }
}