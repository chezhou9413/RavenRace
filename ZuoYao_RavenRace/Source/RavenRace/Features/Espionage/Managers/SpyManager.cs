using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace RavenRace.Features.Espionage.Managers
{
    public static class SpyManager
    {
        public static void DispatchColonist(Pawn pawn, Faction target)
        {
            if (pawn == null || target == null) return;
            var comp = Find.World.GetComponent<WorldComponent_Espionage>();

            SpyData spy = new SpyData(comp.GetNextSpyID());
            spy.InitializeFromPawn(pawn);
            spy.targetFaction = target;
            spy.state = SpyState.Infiltrating;

            comp.AddSpy(spy);

            var factionData = comp.GetSpyData(target);
            factionData.activeSpies.Add(spy);

            if (pawn.Spawned) pawn.DeSpawn();
            if (!Find.WorldPawns.Contains(pawn))
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();

            Messages.Message("RavenRace_Espionage_DispatchSuccess".Translate(pawn.LabelShort, target.Name), MessageTypeDefOf.TaskCompletion);
        }

        public static void RecallSpy(SpyData spy, Map mapToReturn)
        {
            if (spy == null) return;
            var comp = Find.World.GetComponent<WorldComponent_Espionage>();

            if (spy.targetFaction != null)
            {
                var factionData = comp.GetSpyData(spy.targetFaction);
                factionData.activeSpies.Remove(spy);
            }
            comp.RemoveSpy(spy);

            if (spy.sourceType == SpySourceType.Colonist && spy.colonistRef != null)
            {
                Pawn p = spy.colonistRef;
                if (p == null || p.Destroyed) return;

                if (Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);

                if (mapToReturn != null)
                {
                    IntVec3 spot;
                    if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => c.Standable(mapToReturn) && !c.Fogged(mapToReturn), mapToReturn, CellFinder.EdgeRoadChance_Neutral, out spot))
                    {
                        spot = DropCellFinder.TradeDropSpot(mapToReturn);
                    }
                    if (p.holdingOwner != null) p.holdingOwner.Remove(p);
                    GenSpawn.Spawn(p, spot, mapToReturn);
                    p.Drawer?.renderer?.SetAllGraphicsDirty();
                    Messages.Message("RavenRace_Espionage_RecallSuccess".Translate(p.LabelShort), p, MessageTypeDefOf.PositiveEvent);
                }
            }
        }
    }
}