using System.Collections.Generic;
using RimWorld;
using Verse;
using RavenRace.Features.Reproduction; // [Added]

namespace RavenRace
{
    public partial class CompSoulAltar
    {
        public Dictionary<IntVec3, Building_AltarInfuser> connectedInfusers = new Dictionary<IntVec3, Building_AltarInfuser>();
        public Dictionary<IntVec3, Building_AltarInfuser> connectedInjectors = new Dictionary<IntVec3, Building_AltarInfuser>();
        public Dictionary<IntVec3, Building_AltarPylon> connectedPylons = new Dictionary<IntVec3, Building_AltarPylon>();

        public void ScanNetwork()
        {
            if (!parent.Spawned) return;
            Map map = parent.Map;
            IntVec3 center = parent.Position;
            Rot4 rot = parent.Rotation;

            connectedInfusers.Clear();
            connectedInjectors.Clear();
            connectedPylons.Clear();

            foreach (var offset in AltarGeometryUtility.InfuserOffsets)
            {
                IntVec3 pos = center + AltarGeometryUtility.GetRotatedOffset(offset, rot);
                if (pos.InBounds(map))
                {
                    var b = pos.GetFirstThing<Building_AltarInfuser>(map);
                    if (b != null && b.def.defName == "Raven_Altar_Infuser")
                        connectedInfusers.Add(offset, b);
                }
            }

            foreach (var offset in AltarGeometryUtility.InjectorOffsets)
            {
                IntVec3 pos = center + AltarGeometryUtility.GetRotatedOffset(offset, rot);
                if (pos.InBounds(map))
                {
                    var b = pos.GetFirstThing<Building_AltarInfuser>(map);
                    if (b != null && b.def.defName == "Raven_Altar_Injector")
                        connectedInjectors.Add(offset, b);
                }
            }

            foreach (var offset in AltarGeometryUtility.PylonOffsets)
            {
                IntVec3 pos = center + AltarGeometryUtility.GetRotatedOffset(offset, rot);
                if (pos.InBounds(map))
                {
                    var b = pos.GetFirstThing<Building_AltarPylon>(map);
                    if (b != null) connectedPylons.Add(offset, b);
                }
            }
        }

        public float GetSpeedMultiplier()
        {
            int activePylons = 0;
            foreach (var kvp in connectedPylons)
            {
                var power = kvp.Value.GetComp<CompPowerTrader>();
                if (power != null && power.PowerOn) activePylons++;
            }

            float mult = 1.0f + (activePylons * Props.hatchingSpeedPerPylon);
            if (activePylons >= 12) mult += 0.5f;

            if (parent is Building_Cradle cradle && cradle.GetDirectlyHeldThings().Count > 0)
            {
                // [Change] Comp_SpiritEgg -> CompSpiritEgg
                var eggComp = cradle.GetDirectlyHeldThings()[0].TryGetComp<CompSpiritEgg>();
                if (eggComp != null)
                {
                    mult += eggComp.warmthProgress * 0.5f;
                }
            }

            return mult;
        }
    }
}