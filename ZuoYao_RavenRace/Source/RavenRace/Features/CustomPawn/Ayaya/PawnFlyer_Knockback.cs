using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    /// <summary>
    /// 击退专用飞行器。
    /// 这里的逻辑非常简单：它只是一个容器，落地时对内部的人造成伤害和眩晕。
    /// </summary>
    public class PawnFlyer_Knockback : PawnFlyer
    {
        // 落地伤害
        private const int ImpactDamage = 15;
        // 眩晕时长 (Tick)
        private const int StunDuration = 180; // 3秒

        protected override void RespawnPawn()
        {
            // 必须在调用 base.RespawnPawn() 之前获取引用，因为基类会将人从容器中取出
            Pawn p = this.FlyingPawn;

            // 执行落地逻辑 (将人放回地图)
            base.RespawnPawn();

            if (p != null && !p.Dead && p.Spawned)
            {
                // 1. 施加落地伤害 (模拟重摔，Blunt)
                DamageInfo crashDmg = new DamageInfo(DamageDefOf.Blunt, ImpactDamage, 0.1f, -1, null, null, null);
                p.TakeDamage(crashDmg);

                // 2. 施加眩晕 (击倒效果)
                p.stances.stunner.StunFor(StunDuration, null, false, true);

                // 3. 落地灰尘特效
                FleckMaker.ThrowDustPuff(p.Position, p.Map, 2f);
            }
        }
    }
}