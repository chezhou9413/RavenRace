using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using LudeonTK;
using System.Reflection;
using RavenRace.Features.Operator;
using RavenRace.Features.Bloodline;
using RavenRace.Features.RavenRite.Rite_Promotion.Purification.Comps;
using RavenRace.Features.RavenRite.Rite_Promotion.Purification.Defs;

namespace RavenRace
{
    public static class DebugActions
    {
        [DebugAction("渡鸦族", "设置金乌浓度 (10%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationLow(Pawn p) => TrySetConcentration(p, 0.1f);

        [DebugAction("渡鸦族", "重置为纯血渡鸦", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ResetBloodline(Pawn p)
        {
            CompBloodline comp = p.TryGetComp<CompBloodline>();
            CompPurification purComp = p.TryGetComp<CompPurification>();

            if (comp != null)
            {
                BloodlineManager.InitializePawnBloodline(p, comp);
                comp.RefreshAbilities();
                if (purComp != null) purComp.RefreshPurificationBonuses();
                Messages.Message($"已将 {p.LabelShort} 的血脉重置为纯血渡鸦。", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        [DebugAction("渡鸦族", "赋予 1% 异种血脉...", false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddExoticBloodlineTool()
        {
            List<DebugMenuOption> options = new List<DebugMenuOption>();
            List<BloodlineDef> allBloodlines = DefDatabase<BloodlineDef>.AllDefsListForReading;

            foreach (var bDef in allBloodlines)
            {
                string raceDefName = bDef.raceDef?.defName ?? "Unknown";
                string label = $"{bDef.label} ({raceDefName})";

                options.Add(new DebugMenuOption(label, DebugMenuOptionMode.Action, delegate
                {
                    DebugTool tool = null;
                    tool = new DebugTool($"赋予 1% {bDef.label}", delegate
                    {
                        Pawn targetPawn = Find.CurrentMap.thingGrid.ThingAt<Pawn>(UI.MouseCell());
                        if (targetPawn != null) ApplyBloodlineDev(targetPawn, bDef);
                    });
                    DebugTools.curTool = tool;
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        private static void ApplyBloodlineDev(Pawn p, BloodlineDef bDef)
        {
            CompBloodline comp = p.TryGetComp<CompBloodline>();
            if (comp == null)
            {
                Messages.Message($"{p.LabelShort} 没有杂交血脉组件。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            string key = bDef.raceDef.defName;
            if (comp.BloodlineComposition.ContainsKey(key)) comp.BloodlineComposition[key] += 0.01f;
            else comp.BloodlineComposition.Add(key, 0.01f);

            comp.RefreshAbilities();
            p.Drawer?.renderer?.SetAllGraphicsDirty();
            Messages.Message($"已为 {p.LabelShort} 注入 1% {bDef.label}。当前血脉已重新计算。", MessageTypeDefOf.TaskCompletion, false);
        }

        private static void TrySetConcentration(Pawn p, float value)
        {
            CompPurification comp = p.TryGetComp<CompPurification>();
            if (comp != null)
            {
                comp.GoldenCrowConcentration = value;
                comp.RefreshPurificationBonuses();
                Messages.Message($"已将 {p.LabelShort} 的金乌血脉浓度强制设置为 {value:P0}", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        [DebugAction("渡鸦族", "左爻：好感度+100", false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddFavor()
        {
            Features.Operator.WorldComponent_OperatorManager.ChangeFavorability(100);
            Messages.Message("左爻好感度 +100", MessageTypeDefOf.TaskCompletion);
        }

        // ==========================================
        // 新增：用于专门测试“纯化系统”的两个快捷功能
        // ==========================================

        [DebugAction("渡鸦族", "金乌浓度 +5% (受阈值限制)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddConcentrationSafe(Pawn p)
        {
            CompPurification comp = p.TryGetComp<CompPurification>();
            if (comp != null)
            {
                // 使用原装方法，这会受到当前阶段设定的物理极限卡主
                comp.TryAddGoldenCrowConcentration(0.05f, 1.0f);
                Messages.Message($"已提升 {p.LabelShort} 的浓度 (+5%)。当前浓度: {comp.GoldenCrowConcentration:P0} (阶段上限: {comp.GetMaxConcentrationLimit():P0})", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        [DebugAction("渡鸦族", "金乌浓度 +5% (无视限制并自动突破)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddConcentrationForce(Pawn p)
        {
            CompPurification comp = p.TryGetComp<CompPurification>();
            if (comp != null)
            {
                // 强行加上 5%
                comp.GoldenCrowConcentration += 0.05f;

                bool upgraded = false;

                // 若超出当前阈值，一直循环升级阶段，直到达到对应浓度的合适阶段
                while (comp.GoldenCrowConcentration >= comp.GetMaxConcentrationLimit())
                {
                    var allStages = DefDatabase<PurificationStageDef>.AllDefsListForReading;
                    if (comp.currentPurificationStage >= allStages.Max(s => s.stageIndex))
                        break; // 防止无限死循环，如果已经是定义里的最高阶段就停下

                    comp.currentPurificationStage++;
                    upgraded = true;
                }

                // 刷新所有的状态（特质、Hediff、技能）
                comp.RefreshPurificationBonuses();

                if (upgraded)
                {
                    Messages.Message($"强制突破成功！{p.LabelShort} 现已进入阶段 {comp.currentPurificationStage}。当前浓度: {comp.GoldenCrowConcentration:P0}", MessageTypeDefOf.PositiveEvent, false);
                }
                else
                {
                    Messages.Message($"已强制提升 {p.LabelShort} 浓度 (+5%)。当前浓度: {comp.GoldenCrowConcentration:P0}", MessageTypeDefOf.TaskCompletion, false);
                }
            }
        }
    }
}