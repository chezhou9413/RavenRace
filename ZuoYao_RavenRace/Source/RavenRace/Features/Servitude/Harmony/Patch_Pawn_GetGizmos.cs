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
            // 首先返回原版及其他Mod的所有Gizmo
            foreach (var gizmo in __result)
            {
                yield return gizmo;
            }

            // 【核心修改】在这里添加了对 `__instance.RaceProps.Humanlike` 的检查。
            // 这将确保按钮只对类人生物显示，从而过滤掉动物和机械体。
            // 结合原有的派系和存活检查，实现了完整的逻辑闭环。
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
                        // 遍历所有自由殖民者作为候选人
                        foreach (Pawn p in __instance.Map.mapPawns.FreeColonists)
                        {
                            // 不能将自己设为自己的侍奉者
                            if (p != __instance)
                            {
                                Pawn currentMaster = manager.GetMaster(p);
                                if (currentMaster != null)
                                {
                                    // 如果候选人已在侍奉他人，则在菜单中标记并禁用
                                    options.Add(new FloatMenuOption($"{p.LabelShort} (正在侍奉 {currentMaster.LabelShort})", null));
                                }
                                else
                                {
                                    // 否则，添加为可选项
                                    options.Add(new FloatMenuOption(p.LabelShort, () => manager.AddRelation(__instance, p)));
                                }
                            }
                        }
                        // 显示浮动菜单
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };

                // Gizmo 2: 解除关系
                // 只有当该Pawn是主人或侍奉者时，才显示此按钮
                if (manager.IsMaster(__instance) || manager.IsServant(__instance))
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "解除侍奉关系",
                        defaultDesc = "解除该角色当前的所有主从关系。",
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Dismiss", true),
                        action = () => manager.RemoveRelation(__instance)
                    };
                }
            }
        }
    }
}