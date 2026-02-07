using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace
{
    public class CompProperties_SteadyAim : CompProperties
    {
        public HediffDef aimHediff;
        public int ticksToStack = 120;

        public CompProperties_SteadyAim()
        {
            this.compClass = typeof(CompSteadyAim);
        }
    }

    public class CompSteadyAim : ThingComp
    {
        public CompProperties_SteadyAim Props => (CompProperties_SteadyAim)props;
        private int ticksNotMoving = 0;
        private IntVec3 lastPos = IntVec3.Invalid;

        private Pawn Holder => (parent.ParentHolder is Pawn_EquipmentTracker tracker) ? tracker.pawn : null;

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = Holder;

            if (pawn == null || !pawn.Spawned || pawn.equipment?.Primary != parent)
            {
                if (pawn != null) RemoveHediff(pawn);
                return;
            }

            if (pawn.Position != lastPos)
            {
                ticksNotMoving = 0;
                RemoveHediff(pawn);
                lastPos = pawn.Position;
            }
            else
            {
                ticksNotMoving++;
                if (ticksNotMoving >= Props.ticksToStack)
                {
                    AddOrStackHediff(pawn);
                    ticksNotMoving = 0;
                }
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveHediff(pawn);
            ticksNotMoving = 0;
            lastPos = IntVec3.Invalid;
        }

        private void RemoveHediff(Pawn p)
        {
            if (p == null || p.health == null) return;
            Hediff h = p.health.hediffSet.GetFirstHediffOfDef(Props.aimHediff);
            if (h != null)
            {
                p.health.RemoveHediff(h);
                p.Drawer?.renderer?.SetAllGraphicsDirty(); // 核心修改：移除时标记为脏
            }
        }

        private void AddOrStackHediff(Pawn p)
        {
            Hediff h = p.health.hediffSet.GetFirstHediffOfDef(Props.aimHediff);
            if (h == null)
            {
                h = p.health.AddHediff(Props.aimHediff);
                h.Severity = 0.1f;
            }
            else
            {
                if (h.Severity < 1.0f)
                {
                    h.Severity += 0.1f;
                }
            }
            p.Drawer?.renderer?.SetAllGraphicsDirty(); // 核心修改：添加或堆叠时标记为脏
        }
    }
}