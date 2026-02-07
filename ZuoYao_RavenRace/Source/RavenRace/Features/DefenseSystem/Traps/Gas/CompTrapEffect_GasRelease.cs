using Verse;
using RimWorld;
using Verse.Sound;

namespace RavenRace
{
    public class CompProperties_GasRelease : CompProperties
    {
        public ThingDef gasDef;
        public int gasRadius = 3;

        public CompProperties_GasRelease()
        {
            this.compClass = typeof(CompTrapEffect_GasRelease);
        }
    }

    public class CompTrapEffect_GasRelease : CompTrapEffect
    {
        public CompProperties_GasRelease Props => (CompProperties_GasRelease)this.props;

        public override void OnTriggered(Pawn triggerer)
        {
            if (Props.gasDef == null) return;

            Map map = parent.Map;
            IntVec3 pos = parent.Position;

            // [Fixed] 换成 TrapSpring，确保是 OneShot 音效
            SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(pos, map));

            // 2. 在周围释放气体
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pos, Props.gasRadius, true))
            {
                if (cell.InBounds(map) && GenSight.LineOfSight(pos, cell, map, true))
                {
                    if (cell.GetFirstThing(map, Props.gasDef) == null)
                    {
                        GenSpawn.Spawn(Props.gasDef, cell, map);
                    }
                }
            }

            // 3. 销毁陷阱
            parent.Destroy(DestroyMode.KillFinalize);
        }
    }
}