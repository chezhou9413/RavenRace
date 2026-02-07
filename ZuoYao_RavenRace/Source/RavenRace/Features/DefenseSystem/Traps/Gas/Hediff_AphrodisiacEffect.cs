using Verse;
using RimWorld;
using Verse.AI;
using System.Linq;

namespace RavenRace
{
    public class Hediff_AphrodisiacEffect : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();

            if (this.Severity > 0.3f && pawn.IsHashIntervalTick(120))
            {
                StripOneApparel();
            }

            // 强行终止战斗行为
            if (pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.IsHashIntervalTick(30))
            {
                pawn.mindState.enemyTarget = null;

                if (pawn.CurJob != null)
                {
                    bool isLovin = pawn.CurJob.def == RavenDefOf.Raven_Job_ForceLovin;
                    if (!isLovin)
                    {
                        if (pawn.CurJob.def == JobDefOf.AttackMelee ||
                            pawn.CurJob.def == JobDefOf.AttackStatic ||
                            pawn.CurJob.def == JobDefOf.Wait_Combat ||
                            pawn.CurJob.def == JobDefOf.Wait_Wander)
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                    }
                }
            }

            // 昏迷逻辑
            if (this.Severity >= 1.0f)
            {
                if (pawn.stances != null && pawn.stances.stunner != null)
                {
                    pawn.stances.stunner.StunFor(600, null, true, true);
                }
                this.Severity = 0.5f;
            }
        }

        private void StripOneApparel()
        {
            if (pawn.apparel == null || pawn.apparel.WornApparelCount == 0) return;
            Apparel ap = pawn.apparel.WornApparel.RandomElement();
            pawn.apparel.TryDrop(ap, out var resultingAp, pawn.PositionHeld, true);
            if (resultingAp != null)
            {
                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
            }
        }
    }
}