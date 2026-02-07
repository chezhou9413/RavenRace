using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using LudeonTK;
using Verse.AI;
using RavenRace.Compat.MoeLotl;
using HarmonyLib;
using System.Reflection;
using RavenRace.Features.Operator;
using RavenRace.Features.Bloodline; // [Added]

namespace RavenRace
{
    public static class DebugActions
    {
        [DebugAction("渡鸦族", "设置浓度 (10%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationLow(Pawn p) => TrySetConcentration(p, 0.1f);

        [DebugAction("渡鸦族", "设置浓度 (50%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationMedium(Pawn p) => TrySetConcentration(p, 0.5f);

        [DebugAction("渡鸦族", "设置浓度 (90%)", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetConcentrationHigh(Pawn p) => TrySetConcentration(p, 0.9f);

        [DebugAction("渡鸦族", "重置为纯血渡鸦", false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ResetBloodline(Pawn p)
        {
            // [Change] Comp_Bloodline -> CompBloodline
            CompBloodline comp = p.TryGetComp<CompBloodline>();
            if (comp != null)
            {
                BloodlineManager.InitializePawnBloodline(p, comp);
                Messages.Message($"已将 {p.LabelShort} 的血脉重置为纯血渡鸦。", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        [DebugAction("渡鸦族", "左爻：好感度+100", false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddFavor()
        {
            Features.Operator.WorldComponent_OperatorManager.ChangeFavorability(100);
            Messages.Message("左爻好感度 +100", MessageTypeDefOf.TaskCompletion);
        }

        [DebugAction("渡鸦族", "左爻：设置好感度...", false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetFavor()
        {
            List<DebugMenuOption> options = new List<DebugMenuOption>();
            for (int i = -1000; i <= 1000; i += 100)
            {
                int favor = i;
                options.Add(new DebugMenuOption(favor.ToString(), DebugMenuOptionMode.Action, () => {
                    var manager = Find.World.GetComponent<WorldComponent_OperatorManager>();
                    if (manager != null)
                    {
                        manager.zuoYaoFavorability = favor;
                        manager.GetType().GetMethod("CheckForNewRewards", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(manager, new object[] { 0, favor });
                    }
                    Messages.Message($"左爻好感度已设置为: {favor}", MessageTypeDefOf.TaskCompletion);
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        private static void TrySetConcentration(Pawn p, float value)
        {
            // [Change] Comp_Bloodline -> CompBloodline
            CompBloodline comp = p.TryGetComp<CompBloodline>();
            if (comp != null)
            {
                comp.GoldenCrowConcentration = value;
                Messages.Message($"已将 {p.LabelShort} 的金乌血脉浓度设置为 {value:P0}", MessageTypeDefOf.TaskCompletion, false);
            }
        }
    }
}