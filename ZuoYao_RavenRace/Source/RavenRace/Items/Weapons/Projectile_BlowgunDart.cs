using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace
{
    public class Projectile_BlowgunDart : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);

            if (hitThing is Pawn hitPawn && !hitPawn.Dead)
            {
                HediffDef aphrodisiac = DefenseDefOf.RavenHediff_AphrodisiacEffect;
                if (aphrodisiac != null)
                {
                    // 施加 Buff
                    HealthUtility.AdjustSeverity(hitPawn, aphrodisiac, 0.5f);

                    // [修复] 使用 LabelCap 获取正确的翻译名称，而不是硬编码 Key
                    string hediffLabel = aphrodisiac.LabelCap;
                    MoteMaker.ThrowText(hitPawn.DrawPos, hitPawn.Map, hediffLabel, Color.magenta);
                }
            }
        }
    }
}