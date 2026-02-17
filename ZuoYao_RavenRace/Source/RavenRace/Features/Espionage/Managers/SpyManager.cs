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

            if (spy.sourceType == SpySourceType.Colonist)
            {
                Pawn p = spy.colonistRef;
                if (p == null || p.Destroyed)
                {
                    Log.Error($"[RavenRace] RecallSpy: colonistRef 为 null 或已销毁, agentName={spy.agentName}");
                    return;
                }

                if (mapToReturn == null)
                {
                    mapToReturn = Find.AnyPlayerHomeMap;
                    if (mapToReturn == null)
                    {
                        Log.Error("[RavenRace] RecallSpy: 没有可用的玩家地图");
                        return;
                    }
                }

                try
                {
                    if (Find.WorldPawns.Contains(p))
                        Find.WorldPawns.RemovePawn(p);
                    if (p.holdingOwner != null)
                        p.holdingOwner.Remove(p);
                    if (p.Spawned)
                        p.DeSpawn();

                    IntVec3 spot;
                    if (!CellFinder.TryFindRandomEdgeCellWith(
                        c => c.Standable(mapToReturn) && !c.Fogged(mapToReturn),
                        mapToReturn, CellFinder.EdgeRoadChance_Neutral, out spot))
                    {
                        spot = DropCellFinder.TradeDropSpot(mapToReturn);
                    }

                    GenSpawn.Spawn(p, spot, mapToReturn);

                    if (p.Faction != Faction.OfPlayer)
                        p.SetFaction(Faction.OfPlayer);

                    if (p.guest != null)
                        p.guest.SetGuestStatus(null);

                    p.Drawer?.renderer?.SetAllGraphicsDirty();
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RavenRace] RecallSpy: Spawn 失败: {ex}");
                    if (!p.Destroyed && !Find.WorldPawns.Contains(p) && !p.Spawned)
                        Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
                    return;
                }

                if (spy.targetFaction != null)
                {
                    var factionData = comp.GetSpyData(spy.targetFaction);
                    factionData?.activeSpies.Remove(spy);
                }
                comp.RemoveSpy(spy);

                Messages.Message("RavenRace_Espionage_RecallSuccess".Translate(p.LabelShort),
                    p, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}