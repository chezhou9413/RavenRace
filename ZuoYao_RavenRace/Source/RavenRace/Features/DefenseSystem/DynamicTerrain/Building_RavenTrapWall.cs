using Verse;
using RimWorld;

namespace RavenRace
{
    public class Building_RavenTrapWall : Building
    {
        private int ticksLeft = 2500; // 默认约40秒

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
        }

        public void Initialize(int duration)
        {
            this.ticksLeft = duration;
        }

        // [Fixed] 改为 protected override
        protected override void Tick()
        {
            base.Tick();
            if (ticksLeft > 0)
            {
                ticksLeft--;
                if (ticksLeft <= 0)
                {
                    this.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}