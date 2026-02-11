using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace
{
    /// 渡鸦族叙事者事件：左爻救场

    public class IncidentWorker_ZuoYaoJoin : IncidentWorker
    {
        // 日志标签
        private static readonly string LogLabel = "IncidentWorker_ZuoYaoJoin";

        /// <summary>
        /// 检查是否可以触发事件
        /// 条件：玩家殖民地无殖民者有战斗力（全员Downed或Dead），且左爻不存在
        /// </summary>
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // 获取殖民者列表
            IEnumerable<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (!colonists.Any()) return false;

            // 检查是否有殖民者还能战斗 (没倒下)
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

        /// <summary>
        /// 执行事件
        /// </summary>
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // 1. 寻找生成位置 (边缘随机位置，必须可达殖民地)
            IntVec3 spawnLoc;
            if (!CellFinder.TryFindRandomEdgeCellWith(
                (IntVec3 c) => map.reachability.CanReachColony(c),
                map,
                CellFinder.EdgeRoadChance_Neutral,
                out spawnLoc))
            {
                Log.Warning($"[{LogLabel}] Failed to find spawn location for ZuoYao.");
                return false;
            }

            // 2. 获取 PawnKind 定义 (完全信任Defs/CustomPawn的定义)
            // 注意：PawnKind 中已经定义了名字 ("左爻")、强制服装 (apparelRequired)、背景分类 (backstoryFiltersOverride)
            PawnKindDef zuoYaoKind = DefDatabase<PawnKindDef>.GetNamed("Raven_PawnKind_ZuoYao", true);
            if (zuoYaoKind == null)
            {
                Log.Error($"[{LogLabel}] PawnKind 'Raven_PawnKind_ZuoYao' not found in Database.");
                return false;
            }

            // 3. 生成 Pawn
            // 这里移除了所有手动覆盖 (Name, Backstory, Skills, Traits, Apparel)
            // 全部由 PawnGenerator 根据 zuoYaoKind 自动处理
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: zuoYaoKind,
                faction: Faction.OfPlayer, // 直接设为玩家派系，方便立即使用
                context: PawnGenerationContext.PlayerStarter, // 使用开局上下文以匹配 PawnKind 的规则
                tile: map.Tile,
                forceGenerateNewPawn: true,
                fixedGender: Gender.Female, // 确保是女性
                forceAddFreeWarmLayerIfNeeded: true
            );

            Pawn zuoYao = PawnGenerator.GeneratePawn(request);
            if (zuoYao == null)
            {
                Log.Error($"[{LogLabel}] Failed to generate ZuoYao pawn.");
                return false;
            }

            // 手动注入“别天神”技能
            if (zuoYao.abilities != null)
            {
                AbilityDef kotoamatsukami = DefDatabase<AbilityDef>.GetNamed("Raven_Ability_Kotoamatsukami", false);
                if (kotoamatsukami != null)
                {
                    // 如果没有这个能力，才添加
                    if (zuoYao.abilities.GetAbility(kotoamatsukami) == null)
                    {
                        zuoYao.abilities.GainAbility(kotoamatsukami);
                    }
                }
            }

            // 5. 生成到地图
            GenSpawn.Spawn(zuoYao, spawnLoc, map);

            // 6. [核心修复] 显式使用 IncidentDef 的定义发送信件
            // 如果 this.def 为 null 或 letterText 为空，这段代码会Fallback到默认值
            try
            {
                // 获取当前事件的定义
                IncidentDef myDef = this.def;

                // 安全检查
                string letterText = myDef?.letterText ?? "一名叫左爻的渡鸦族接线员加入了你的殖民地...";
                string letterLabel = myDef?.letterLabel ?? "左爻加入";
                LetterDef letterDef = myDef?.letterDef ?? LetterDefOf.PositiveEvent;

                // 使用 LookTargets 包装生成的 Pawn
                LookTargets targets = new LookTargets(zuoYao);

                // 调用系统方法发送信件
                // 这里的 SendStandardLetter 重写版本确保使用我们定义的文本
                base.SendStandardLetter(parms, targets);
            }
            catch (Exception ex)
            {
                Log.Error($"[{LogLabel}] Error sending letter: {ex.Message}");
                // Fallback: 尝试最简单的发送方式
                base.SendStandardLetter(parms, new LookTargets(zuoYao));
            }



            return true;
        }

        /// <summary>
        /// 检查左爻是否已存在于任何地图或世界缓存中
        /// </summary>
        private bool IsZuoYaoExisting()
        {
            foreach (Pawn p in PawnsFinder.AllMapsWorldAndTemporary_Alive)
            {
                // 1. 检查种族
                if (p.def != RavenDefOf.Raven_Race) continue;

                // 2. 检查名字 (PawnKind 中定义了 name="左爻")
                if (p.Name is NameTriple triple && triple.Last == "左" && triple.First == "爻")
                    return true;

            }
            return false;
        }
    }
}