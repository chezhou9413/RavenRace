using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Features.dick
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_OrbitSwords
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var gizmo in __result)
            {
                yield return gizmo;
            }
            if (__instance == null || __instance.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            Comp_OrbitSwords orbitComp = null;
            if (__instance.equipment != null)
            {
                foreach (var eq in __instance.equipment.AllEquipmentListForReading)
                {
                    orbitComp = eq.TryGetComp<Comp_OrbitSwords>();
                    if (orbitComp != null) break;
                }
            }
            //如果武器没有，检查穿戴的衣服
            if (orbitComp == null && __instance.apparel != null)
            {
                foreach (var ap in __instance.apparel.WornApparel)
                {
                    orbitComp = ap.TryGetComp<Comp_OrbitSwords>();
                    if (orbitComp != null) break;
                }
            }

            //如果找到了该组件，并且XML填了Hediff
            if (orbitComp != null)
            {
                if (!orbitComp.IsHidden())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "归元入体",
                        defaultDesc = "立刻无视所有状态机，让飞剑钻入小人后庭暂时消失，并获得强化状态。",
                        icon = ContentFinder<Texture2D>.Get("UI/Abilities/Raven_WanJianGuiZong"), // 可替换为: ContentFinder<Texture2D>.Get("UI/YourCustomIcon")
                        action = delegate
                        {
                            orbitComp.TriggerHide();
                        }
                    };
                }
            }
        }
    }
}
