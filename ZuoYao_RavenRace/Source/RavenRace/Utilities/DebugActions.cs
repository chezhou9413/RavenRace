using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using LudeonTK;
using System.Reflection;
using RavenRace.Features.Operator;
using RavenRace.Features.Bloodline;
using RavenRace.Features.RavenRite.Rite_Promotion.Purification.Comps; // [新增引用]

namespace RavenRace
{
    public static class DebugActions
    {
        [DebugAction("渡鸦族", "设置金乌浓度 (10%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationLow(Pawn p) => TrySetConcentration(p, 0.1f);

        [DebugAction("渡鸦族", "设置金乌浓度 (50%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationMedium(Pawn p) => TrySetConcentration(p, 0.5f);

        [DebugAction("渡鸦族", "设置金乌浓度 (100%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationHigh(Pawn p) => TrySetConcentration(p, 1.0f);

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
            // 修改为操作新的纯化组件
            CompPurification comp = p.TryGetComp<CompPurification>();
            if (comp != null)
            {
                // DEBUG工具无视阶段突破上限强制设置
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
    }
}