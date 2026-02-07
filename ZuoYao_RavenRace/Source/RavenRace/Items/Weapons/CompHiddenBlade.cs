using Verse;
using RimWorld;

namespace RavenRace
{
    public class CompProperties_HiddenBlade : CompProperties
    {
        public int ticksToPrep = 600;
        public HediffDef prepHediff;

        public CompProperties_HiddenBlade()
        {
            this.compClass = typeof(CompHiddenBlade);
        }
    }

    public class CompHiddenBlade : ThingComp
    {
        public CompProperties_HiddenBlade Props => (CompProperties_HiddenBlade)props;
        private int lastAttackTick = -9999;

        private Pawn Holder => (parent.ParentHolder is Pawn_EquipmentTracker tracker) ? tracker.pawn : null;

        public override void CompTick()
        {
            base.CompTick();
            if (!(this.parent.ParentHolder is Pawn_EquipmentTracker)) return;

            Pawn pawn = Holder;
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.equipment?.Primary != parent)
            {
                RemoveBuff(pawn);
                return;
            }

            if (!pawn.health.hediffSet.HasHediff(Props.prepHediff))
            {
                if (Find.TickManager.TicksGame - lastAttackTick > Props.ticksToPrep)
                {
                    pawn.health.AddHediff(Props.prepHediff);
                    pawn.Drawer?.renderer?.SetAllGraphicsDirty(); // 核心修改：添加时标记为脏
                }
            }
        }

        public override void Notify_UsedWeapon(Pawn pawn)
        {
            base.Notify_UsedWeapon(pawn);
            lastAttackTick = Find.TickManager.TicksGame;
            RemoveBuff(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveBuff(pawn);
        }

        private void RemoveBuff(Pawn pawn)
        {
            if (pawn == null || pawn.health == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(Props.prepHediff);
            if (h != null)
            {
                pawn.health.RemoveHediff(h);
                pawn.Drawer?.renderer?.SetAllGraphicsDirty(); // 核心修改：移除时标记为脏
            }
        }
    }
}