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

            if (__instance.Faction == Faction.OfPlayer && !__instance.Dead)
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

                // Gizmo 2: 解除关系
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