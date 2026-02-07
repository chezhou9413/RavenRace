using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Verse.AI.Group;
using UnityEngine;

namespace RavenRace.Features.FusangOrganization.Trader
{
    /// <summary>
    /// 扶桑商队抵达事件处理器
    /// </summary>
    public class IncidentWorker_FusangTraderArrival : IncidentWorker_TraderCaravanArrival
    {
        // 1. 强制允许隐藏派系作为商队来源
        public override bool FactionCanBeGroupSource(Faction f, IncidentParms parms, bool desperate = false)
        {
            if (f.def.defName == "Fusang_Hidden") return true;
            return base.FactionCanBeGroupSource(f, parms, desperate);
        }

        // 2. 核心执行逻辑
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Log.Message("[RavenRace Debug] 开始执行扶桑商队事件 (v1.6 Fix)...");

            Map map = (Map)parms.target;
            if (map == null) return false;

            // ========================================================
            // 步骤 A: 参数强制解析与兜底
            // ========================================================

            if (parms.faction == null)
            {
                parms.faction = Find.FactionManager.FirstFactionOfDef(FusangDefOf.Fusang_Hidden);
                if (parms.faction == null) return false;
            }

            if (parms.traderKind == null)
            {
                parms.traderKind = DefDatabase<TraderKindDef>.GetNamedSilentFail("Raven_Trader_Caravan");
                if (parms.traderKind == null) return false;
            }

            if (parms.points <= 0)
            {
                parms.points = TraderCaravanUtility.GenerateGuardPoints();
                if (parms.points < 500) parms.points = 500;
            }

            // ========================================================
            // 步骤 B: 寻找生成点
            // ========================================================

            if (!RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral, false, null))
            {
                if (!RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral, true, null))
                {
                    return false;
                }
            }

            // ========================================================
            // 步骤 C: 生成 Pawn
            // ========================================================

            PawnGroupMakerParms groupParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Trader, parms, true);
            groupParms.traderKind = parms.traderKind;

            List<Pawn> pawns = PawnGroupMakerUtility.GeneratePawns(groupParms, true).ToList();

            if (pawns == null || pawns.Count == 0) return false;

            // ========================================================
            // 步骤 D: 投放、处理商人与特殊装备逻辑
            // ========================================================

            Pawn mainTrader = null;

            foreach (Pawn p in pawns)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5, null);
                GenSpawn.Spawn(p, loc, map, WipeMode.Vanish);

                if (p.needs?.food != null) p.needs.food.CurLevel = p.needs.food.MaxLevel;
                if (p.needs?.rest != null) p.needs.rest.CurLevel = p.needs.rest.MaxLevel;

                if (mainTrader == null && p.TraderKind != null)
                {
                    mainTrader = p;
                }

                // [新增] 检查并装备特殊武器 (灵卵拉珠)
                CheckAndEquipSpecialWeapons(p);
            }

            if (mainTrader == null && pawns.Count > 0)
            {
                mainTrader = pawns[0];
                if (mainTrader.trader == null)
                {
                    mainTrader.trader = new Pawn_TraderTracker(mainTrader);
                }
                mainTrader.trader.traderKind = parms.traderKind;
            }

            // ========================================================
            // 步骤 E: 指派 AI (Lord)
            // ========================================================

            IntVec3 chillSpot;
            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(parms.spawnCenter, map, mainTrader ?? pawns[0], out chillSpot, (IntVec3 c) => true))
            {
                chillSpot = DropCellFinder.TradeDropSpot(map);
            }

            var lordJob = new LordJob_TradeWithColony(parms.faction, chillSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, pawns);

            // ========================================================
            // 步骤 F: 发信
            // ========================================================

            string label = "LetterLabelTraderCaravanArrival".Translate(parms.faction.Name, parms.traderKind.label).CapitalizeFirst();
            string text = "LetterTraderCaravanArrival".Translate(parms.faction.NameColored, parms.traderKind.label).CapitalizeFirst();
            text += "\n\n这是来自扶桑组织的隐秘物流支援。";

            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, pawns[0], parms.faction);

            return true;
        }

        /// <summary>
        /// 检查Pawn是否应该装备“灵卵拉珠”。
        /// 规则：
        /// 1. 如果背景故事是“拉珠剑大师”(Raven_Adulthood_BeadSwordMaster)，100% 装备。
        /// 2. 否则，有 5% 的概率随机装备。
        /// </summary>
        private void CheckAndEquipSpecialWeapons(Pawn p)
        {
            if (p == null || p.equipment == null) return;

            // 获取背景故事DefName (注意判空)
            string adulthoodDefName = p.story?.Adulthood?.defName;

            bool shouldEquipBeads = false;

            // 判定逻辑
            if (adulthoodDefName == "Raven_Adulthood_BeadSwordMaster")
            {
                shouldEquipBeads = true;
            }
            else if (Rand.Chance(0.05f)) // 5% 彩蛋概率
            {
                shouldEquipBeads = true;
            }

            // 执行装备
            if (shouldEquipBeads)
            {
                ThingDef beadsDef = DefDatabase<ThingDef>.GetNamedSilentFail("Raven_Weapon_SpiritBeads");
                if (beadsDef != null)
                {
                    // 销毁原有武器，腾出位置
                    p.equipment.DestroyAllEquipment();

                    // 生成并装备新武器
                    ThingWithComps beads = (ThingWithComps)ThingMaker.MakeThing(beadsDef, GenStuff.RandomStuffFor(beadsDef));
                    if (beads != null)
                    {
                        // 确保没有被销毁
                        if (p.equipment.Contains(beads)) return; // 理论上不可能，因为是新生成的
                        p.equipment.AddEquipment(beads);

                        // 可选：如果是大师，可以提升武器品质
                        if (adulthoodDefName == "Raven_Adulthood_BeadSwordMaster")
                        {
                            beads.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Excellent, ArtGenerationContext.Outsider);
                        }
                    }
                }
            }
        }
    }
}