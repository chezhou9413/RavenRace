using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
// [核心修改] 引用左爻的独立命名空间，以获取 PawnKind 和 Ability 的 DefOf
using RavenRace.Features.CustomPawn.ZuoYao;

namespace RavenRace.Features.Storyteller.Incidents
{
    /// <summary>
    /// 左爻救场事件 Worker
    /// 逻辑：当玩家陷入绝境时，左爻作为强力支援加入。
    /// 此文件保留在 Storyteller 模块，因为它是叙事者触发的事件。
    /// </summary>
    public class IncidentWorker_ZuoYaoJoin : IncidentWorker
    {
        private static readonly string LogLabel = "IncidentWorker_ZuoYaoJoin";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // 获取殖民者列表
            IEnumerable<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (!colonists.Any()) return false;

            // 检查是否有殖民者还能战斗 (没倒下且没死)
            bool allDownOrDead = true;
            foreach (Pawn p in colonists)
            {
                if (!p.Downed && !p.Dead)
                {
                    allDownOrDead = false;
                    break;
                }
            }

            if (!allDownOrDead) return false;

            // 检查左爻是否已经存在
            if (IsZuoYaoExisting()) return false;

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // 1. 寻找生成位置
            IntVec3 spawnLoc;
            if (!CellFinder.TryFindRandomEdgeCellWith(
                (IntVec3 c) => map.reachability.CanReachColony(c),
                map,
                CellFinder.EdgeRoadChance_Neutral,
                out spawnLoc))
            {
                return false;
            }

            // 2. 获取 PawnKind (使用新定义的 ZuoYaoDefOf)
            PawnKindDef kind = ZuoYaoDefOf.Raven_PawnKind_ZuoYao;
            if (kind == null)
            {
                Log.Error($"[{LogLabel}] PawnKind not found.");
                return false;
            }

            // 3. 生成 Pawn
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: kind,
                faction: Faction.OfPlayer,
                context: PawnGenerationContext.PlayerStarter,
                tile: map.Tile,
                forceGenerateNewPawn: true,
                fixedGender: Gender.Female,
                forceAddFreeWarmLayerIfNeeded: true
            );

            Pawn zuoYao = PawnGenerator.GeneratePawn(request);
            if (zuoYao == null) return false;

            // 4. 确保拥有“别天神”能力
            // 虽然 PawnKind 可能已配置，但作为特殊角色双重保险
            if (zuoYao.abilities != null && ZuoYaoDefOf.Raven_Ability_Kotoamatsukami != null)
            {
                if (zuoYao.abilities.GetAbility(ZuoYaoDefOf.Raven_Ability_Kotoamatsukami) == null)
                {
                    zuoYao.abilities.GainAbility(ZuoYaoDefOf.Raven_Ability_Kotoamatsukami);
                }
            }

            // 5. 生成到地图
            GenSpawn.Spawn(zuoYao, spawnLoc, map);

            // 6. 发送信件
            base.SendStandardLetter(parms, new LookTargets(zuoYao));

            return true;
        }

        private bool IsZuoYaoExisting()
        {
            foreach (Pawn p in PawnsFinder.AllMapsWorldAndTemporary_Alive)
            {
                if (p.kindDef == ZuoYaoDefOf.Raven_PawnKind_ZuoYao) return true;

                // 双重保险：检查名字
                if (p.Name is NameTriple triple && triple.Last == "左" && triple.First == "爻")
                    return true;
            }
            return false;
        }
    }
}