using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using LudeonTK;
using System.Reflection;
using RavenRace.Features.Operator;
using RavenRace.Features.Bloodline;

namespace RavenRace
{
    public static class DebugActions
    {
        // --- 浓度设置工具 (保留原有功能) ---
        [DebugAction("渡鸦族", "设置浓度 (10%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationLow(Pawn p) => TrySetConcentration(p, 0.1f);

        [DebugAction("渡鸦族", "设置浓度 (50%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationMedium(Pawn p) => TrySetConcentration(p, 0.5f);

        [DebugAction("渡鸦族", "设置浓度 (90%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationHigh(Pawn p) => TrySetConcentration(p, 0.9f);

        [DebugAction("渡鸦族", "重置为纯血渡鸦", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ResetBloodline(Pawn p)
        {
            CompBloodline comp = p.TryGetComp<CompBloodline>();
            if (comp != null)
            {
                BloodlineManager.InitializePawnBloodline(p, comp);
                comp.RefreshAbilities(); // 强制刷新
                Messages.Message($"已将 {p.LabelShort} 的血脉重置为纯血渡鸦。", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        // --- [核心修改] 赋予异种血脉工具：先选血脉，后点人 ---
        [DebugAction("渡鸦族", "赋予 1% 异种血脉...", false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddExoticBloodlineTool()
        {
            List<DebugMenuOption> options = new List<DebugMenuOption>();

            // 获取所有定义的血脉
            List<BloodlineDef> allBloodlines = DefDatabase<BloodlineDef>.AllDefsListForReading;

            foreach (var bDef in allBloodlines)
            {
                string raceDefName = bDef.raceDef?.defName ?? "Unknown";
                string label = $"{bDef.label} ({raceDefName})";

                options.Add(new DebugMenuOption(label, DebugMenuOptionMode.Action, delegate
                {
                    // 设置当前调试工具为“点击 Pawn 赋予”
                    DebugTool tool = null;
                    tool = new DebugTool($"赋予 1% {bDef.label}", delegate
                    {
                        // 获取点击位置的 Pawn
                        Pawn targetPawn = Find.CurrentMap.thingGrid.ThingAt<Pawn>(UI.MouseCell());
                        if (targetPawn != null)
                        {
                            ApplyBloodlineDev(targetPawn, bDef);
                        }
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
                Messages.Message($"{p.LabelShort} 没有血脉组件。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            string key = bDef.raceDef.defName;
            if (comp.BloodlineComposition.ContainsKey(key))
            {
                comp.BloodlineComposition[key] += 0.01f;
            }
            else
            {
                comp.BloodlineComposition.Add(key, 0.01f);
            }

            // 关键：强制执行 1% 保底归一化并刷新状态
            comp.RefreshAbilities();
            p.Drawer?.renderer?.SetAllGraphicsDirty();

            Messages.Message($"已为 {p.LabelShort} 注入 1% {bDef.label}。当前血脉已重新计算。", MessageTypeDefOf.TaskCompletion, false);
        }

        private static void TrySetConcentration(Pawn p, float value)
        {
            CompBloodline comp = p.TryGetComp<CompBloodline>();
            if (comp != null)
            {
                comp.GoldenCrowConcentration = value;
                comp.RefreshAbilities();
                Messages.Message($"已将 {p.LabelShort} 的金乌血脉浓度设置为 {value:P0}", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        // --- 左爻好感度调试 (保持不变) ---
        [DebugAction("渡鸦族", "左爻：好感度+100", false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddFavor()
        {
            Features.Operator.WorldComponent_OperatorManager.ChangeFavorability(100);
            Messages.Message("左爻好感度 +100", MessageTypeDefOf.TaskCompletion);
        }
    }
}