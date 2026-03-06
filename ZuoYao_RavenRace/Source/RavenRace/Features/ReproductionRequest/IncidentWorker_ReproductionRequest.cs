using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace RavenRace.Features.ReproductionRequest
{
    /// <summary>
    /// 事件触发器：严格限制仅左爻叙事者可触发，并检测是否有男性存活。
    /// </summary>
    public class IncidentWorker_ReproductionRequest : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            // 1. 强制判断叙事者，如果不是左爻，坚决不触发
            if (Find.Storyteller.def.defName != "ZuoYao_Storyteller") return false;

            Map map = (Map)parms.target;
            if (map == null || !map.IsPlayerHome) return false;

            // 2. 判断殖民地内是否有至少一名活着的、未倒地的男性
            bool hasValidMale = map.mapPawns.FreeColonistsSpawned.Any(p => p.gender == Gender.Male && !p.Downed && !p.Dead);
            if (!hasValidMale) return false;

            // 3. 判断是否能找到安全的生成点
            return CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => c.Standable(map) && map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // 找生成点
            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => c.Standable(map) && map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out IntVec3 spawnSpot))
            {
                return false;
            }

            // 找殖民地外围的集结闲逛点
            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(spawnSpot, map, out IntVec3 chillSpot))
            {
                chillSpot = spawnSpot;
            }

            Faction fusangFaction = Find.FactionManager.FirstFactionOfDef(RavenDefOf.Fusang_Hidden);
            if (fusangFaction == null) return false;

            int pawnCount = Rand.RangeInclusive(6, 8);
            List<Pawn> group = new List<Pawn>();

            // 生成 6-8 名女性渡鸦
            for (int i = 0; i < pawnCount; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: RavenDefOf.Raven_Colonist,
                    faction: fusangFaction,
                    context: PawnGenerationContext.NonPlayer,
                    fixedBiologicalAge: Rand.Range(14f, 28f),
                    fixedGender: Gender.Female,
                    forceGenerateNewPawn: true
                );

                Pawn p = PawnGenerator.GeneratePawn(request);

                // [修复] 赋予催情 Hediff，底层的 ThoughtWorker 会自动识别并产生色色的想法
                if (RavenDefOf.RavenHediff_AphrodisiacEffect != null)
                {
                    Hediff h = p.health.AddHediff(RavenDefOf.RavenHediff_AphrodisiacEffect);
                    h.Severity = 0.5f;
                }

                group.Add(p);
            }

            // 随机选出队长
            Pawn leader = group.RandomElement();

            // 生成并分配主状态机 Lord
            LordJob_ReproductionRequest lordJob = new LordJob_ReproductionRequest(leader, chillSpot);
            LordMaker.MakeNewLord(fusangFaction, lordJob, map, group);

            // 投放进地图
            foreach (Pawn pawn in group)
            {
                IntVec3 loc = CellFinder.RandomSpawnCellForPawnNear(spawnSpot, map, 4);
                GenSpawn.Spawn(pawn, loc, map, Rot4.Random);
            }

            base.SendStandardLetter(parms, new LookTargets(leader));
            return true;
        }
    }
}