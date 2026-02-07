using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace
{
    public class Building_RavenTrap : Building
    {
        private List<Pawn> touchingPawns = new List<Pawn>();

        public bool IsArmed
        {
            get
            {
                var trigger = GetComp<CompTrigger>();
                return trigger != null && trigger.IsArmed && !trigger.OnCooldown;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref touchingPawns, "touchingPawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && touchingPawns == null)
            {
                touchingPawns = new List<Pawn>();
            }
        }

        // [Check] 确保这里的修饰符与基类一致。通常是 public。
        protected override void Tick()
        {
            base.Tick(); // 这里调用基类 Tick

            if (this.Spawned && IsArmed)
            {
                List<Thing> thingList = this.Position.GetThingList(this.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Pawn pawn = thingList[i] as Pawn;
                    if (pawn != null && !touchingPawns.Contains(pawn))
                    {
                        touchingPawns.Add(pawn);
                        CheckSpring(pawn);
                    }
                }

                for (int j = touchingPawns.Count - 1; j >= 0; j--)
                {
                    Pawn pawn2 = touchingPawns[j];
                    if (pawn2 == null || !pawn2.Spawned || pawn2.Position != this.Position)
                    {
                        touchingPawns.RemoveAt(j);
                    }
                }
            }
        }

        private void CheckSpring(Pawn p)
        {
            var trigger = GetComp<CompTrigger>();
            if (trigger != null)
            {
                trigger.Notify_SteppedOn(p);
            }
        }
    }
}