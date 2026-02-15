using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    /// <summary>
    /// 带拖尾的爆炸投射物
    /// </summary>
    // [核心修复] 继承自 Projectile_Explosive 以支持 XML 中的 <damageDef>Bomb</damageDef> 和范围伤害
    public class Projectile_WithTrail : Projectile_Explosive
    {
        protected override void Tick()
        {
            // 必须调用 base.Tick()，否则不会飞行，也不会倒计时爆炸
            base.Tick();

            // 生成妖灵拖尾
            if (this.IsHashIntervalTick(2) && this.Map != null)
            {
                if (BinahDefOf.Raven_Mote_Binah_FairyTrail != null)
                {
                    Vector3 drawPos = this.DrawPos;
                    // 稍微随机化位置
                    drawPos += new Vector3(Rand.Range(-0.2f, 0.2f), 0, Rand.Range(-0.2f, 0.2f));

                    // 绘制拖尾 Mote
                    Mote mote = MoteMaker.MakeStaticMote(drawPos, this.Map, BinahDefOf.Raven_Mote_Binah_FairyTrail, 2.0f); // 拖尾也大一点
                    if (mote != null)
                    {
                        mote.rotationRate = Rand.Range(-20f, 20f);
                    }
                }
            }
        }

    }
}