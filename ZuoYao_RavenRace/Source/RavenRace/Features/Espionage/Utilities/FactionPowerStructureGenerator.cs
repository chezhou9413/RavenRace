using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using AlienRace;

namespace RavenRace.Features.Espionage.Utilities
{
    /// <summary>
    /// 负责生成和管理派系的权力结构（官员数据）。
    /// </summary>
    public static class FactionPowerStructureGenerator
    {
        public static void GenerateStructureFor(Faction faction, FactionSpyData data, ref int nextID)
        {
            if (faction == null || data == null) return;

            data.allOfficials.Clear();

            // [核心修复] 预先获取该派系所有可用的种族 PawnKind 列表
            // 这样我们在生成每个官员时，可以随机分配不同的身份（公民、士兵、精锐等）
            List<PawnKindDef> availableKinds = GetFactionPawnKinds(faction.def);

            // 1. 领袖 (优先使用现有的 faction.leader)
            data.leaderOfficial = CreateOfficial(faction, OfficialRank.Leader, ref nextID, availableKinds, faction.leader);
            data.allOfficials.Add(data.leaderOfficial);

            // 2. 结构规模
            int highCouncilCount = (faction.def.techLevel >= TechLevel.Industrial) ? 5 : 3;
            int middleManagerCount = highCouncilCount * 2;

            // 3. 核心层
            for (int i = 0; i < highCouncilCount; i++)
            {
                OfficialData councilMember = CreateOfficial(faction, OfficialRank.HighCouncil, ref nextID, availableKinds);
                data.leaderOfficial.subordinates.Add(councilMember);
                data.allOfficials.Add(councilMember);
            }

            // 4. 中层干部
            for (int i = 0; i < middleManagerCount; i++)
            {
                OfficialData manager = CreateOfficial(faction, OfficialRank.MiddleManager, ref nextID, availableKinds);
                if (data.leaderOfficial.subordinates.Count > 0)
                {
                    data.leaderOfficial.subordinates.RandomElement().subordinates.Add(manager);
                }
                else
                {
                    data.leaderOfficial.subordinates.Add(manager);
                }
                data.allOfficials.Add(manager);
            }

            RavenModUtility.LogVerbose($"[RavenRace] 为 {faction.Name} 生成了权力结构，共 {data.allOfficials.Count} 名官员。");
        }

        private static OfficialData CreateOfficial(Faction faction, OfficialRank rank, ref int nextID, List<PawnKindDef> availableKinds, Pawn existingPawn = null)
        {
            OfficialData official = new OfficialData(nextID++);
            official.rank = rank;
            official.factionRef = faction;

            official.loyalty = Rand.Range(30f, 100f);
            official.corruption = Rand.Range(0f, 60f);
            official.competence = Rand.Range(20f, 90f);
            official.isKnown = (rank == OfficialRank.Leader);

            Pawn pawnToProcess = existingPawn;
            bool isVirtual = false;

            if (pawnToProcess == null)
            {
                // [核心修复] 从可用列表中随机选择一个 PawnKind
                // 如果是核心层或领袖，尽量选 combatPower 高的（如果有的话），或者直接随机
                PawnKindDef kind = PawnKindDefOf.Colonist; // 默认兜底

                if (!availableKinds.NullOrEmpty())
                {
                    // 简单的权重选择：稍微倾向于战斗力高的作为高层，但也允许平民
                    // 或者完全随机以增加多样性
                    kind = availableKinds.RandomElement();
                }

                // 确保有背景故事，允许 HAR 处理年龄
                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: kind,
                    faction: faction,
                    context: PawnGenerationContext.NonPlayer,
                    tile: -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: false,
                    colonistRelationChanceFactor: 0f,
                    forceAddFreeWarmLayerIfNeeded: true,
                    forceNoBackstory: false
                );

                try
                {
                    // 消耗随机数种子
                    int seed = Rand.Int;
                    pawnToProcess = PawnGenerator.GeneratePawn(request);
                    isVirtual = true;
                }
                catch (Exception ex)
                {
                    Log.Error($"[RavenRace] Error generating virtual official for {faction.Name}: {ex}");
                    request.KindDef = PawnKindDefOf.Colonist;
                    pawnToProcess = PawnGenerator.GeneratePawn(request);
                    isVirtual = true;
                }
            }

            // 提取数据
            OfficialPawnUtility.SnapshotPawnData(official, pawnToProcess);

            if (isVirtual && pawnToProcess != null && !pawnToProcess.Destroyed)
            {
                if (Find.WorldPawns.Contains(pawnToProcess))
                {
                    Find.WorldPawns.RemovePawn(pawnToProcess);
                }
                pawnToProcess.Discard();
            }
            else if (existingPawn != null)
            {
                official.pawnReference = existingPawn;
            }

            return official;
        }

        /// <summary>
        /// [核心修复逻辑] 获取该派系所有合法的、属于主体种族的 PawnKind。
        /// </summary>
        private static List<PawnKindDef> GetFactionPawnKinds(FactionDef facDef)
        {
            List<PawnKindDef> result = new List<PawnKindDef>();

            // 1. 收集所有可能的 PawnKind
            List<PawnKindDef> allKinds = new List<PawnKindDef>();

            if (facDef.pawnGroupMakers != null)
            {
                allKinds.AddRange(facDef.pawnGroupMakers
                    .SelectMany(pgm => pgm.options)
                    .Select(opt => opt.kind));
            }
            if (facDef.basicMemberKind != null)
            {
                allKinds.Add(facDef.basicMemberKind);
            }

            // 去重
            allKinds = allKinds.Distinct().ToList();

            if (allKinds.NullOrEmpty())
            {
                result.Add(PawnKindDefOf.Colonist);
                return result;
            }

            // 2. 找到出现频率最高的主体种族 (ThingDef)
            // 忽略 Human，除非全是 Human
            var raceCounts = allKinds
                .GroupBy(k => k.race)
                .OrderByDescending(g => g.Count());

            ThingDef dominantRace = raceCounts.First().Key;

            // 如果第一名是人类，但列表里有外星人，且该派系定义不是基础玩家派系，尝试找外星人
            // 这是一个启发式判断：如果米莉拉派系里混了人类，我们想生成米莉拉人
            var alienGroup = raceCounts.FirstOrDefault(g => g.Key.defName != "Human");
            if (alienGroup != null)
            {
                dominantRace = alienGroup.Key;
            }

            // 3. 筛选出属于该主体种族的 PawnKind
            // 排除 factionLeader 类型（如“米莉拉女皇”），因为官员不应该全是女皇
            result = allKinds
                .Where(k => k.race == dominantRace && !k.factionLeader)
                .ToList();

            // 4. 如果筛选后为空（比如只有 Leader），则放宽限制
            if (result.Count == 0)
            {
                result = allKinds.Where(k => k.race == dominantRace).ToList();
            }

            // 5. 兜底
            if (result.Count == 0)
            {
                result.Add(PawnKindDefOf.Colonist);
            }

            return result;
        }
    }
}