using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.Servitude.Harmony
{
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_Pawn_GetGizmos
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var gizmo in __result)
            {
                yield return gizmo;
            }

            if (__instance.RaceProps.Humanlike && __instance.Faction == Faction.OfPlayer && !__instance.Dead)
            {
                var manager = ServitudeManager.Get();
                if (manager == null) yield break;

                // Gizmo 1: 建立侍奉关系
                yield return new Command_Action
                {
                    defaultLabel = "建立侍奉关系...",
                    defaultDesc = "指定一个殖民者成为这个角色的专属侍奉者。",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Servitude/ServitudeBond_Icon"),
                    action = () =>
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (Pawn p in __instance.Map.mapPawns.FreeColonists)
                        {
                            if (p != __instance)
                            {
                                Pawn currentMaster = manager.GetMaster(p);
                                if (currentMaster != null)
                                {
                                    options.Add(new FloatMenuOption($"{p.LabelShort} (正在侍奉 {currentMaster.LabelShort})", null));
                                }
                                else
                                {
                                    options.Add(new FloatMenuOption(p.LabelShort, () => manager.AddRelation(__instance, p)));
                                }
                            }
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };

                // Gizmo 2: 解除关系 (主人视角)
                if (manager.IsMaster(__instance))
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "解除侍奉关系",
                        defaultDesc = "解除与指定侍奉者的主从关系。",
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Dismiss", true),
                        action = () =>
                        {
                            List<FloatMenuOption> options = new List<FloatMenuOption>();
                            var servants = manager.GetServants(__instance);

                            // 逐个解除菜单
                            foreach (var s in servants)
                            {
                                // 使用本地变量闭包，防止循环赋值问题
                                Pawn targetServant = s;
                                options.Add(new FloatMenuOption($"解除对 {targetServant.LabelShort} 的支配", () => manager.RemoveRelation(targetServant)));
                            }

                            // 如果有多个奴隶，额外提供一个全部解除的选项
                            if (servants.Count > 1)
                            {
                                options.Add(new FloatMenuOption("解除所有侍奉者", () => manager.RemoveAllServants(__instance)));
                            }

                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                    };
                }
                // Gizmo 2: 解除关系 (侍奉者视角)
                else if (manager.IsServant(__instance))
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "解除侍奉关系",
                        defaultDesc = "停止对主人的侍奉。",
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Dismiss", true),
                        action = () => manager.RemoveRelation(__instance)
                    };
                }
            }
        }
    }
}