using HarmonyLib;
using Verse;
using RimWorld;

namespace RavenRace
{
    [HarmonyPatch(typeof(Pawn), "TickRare")] // 每 250 tick 执行一次，性能友好
    public static class Patch_Pawn_Tick_GasCheck
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (!__instance.Spawned || __instance.Map == null) return;

            // 获取当前格子的所有气体
            var things = __instance.Position.GetThingList(__instance.Map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t.def == DefenseDefOf.RavenGas_Anesthetic)
                {
                    // 增加麻醉 Hediff
                    HealthUtility.AdjustSeverity(__instance, DefenseDefOf.RavenHediff_AnestheticBuildup, 0.15f); // 每次+15%
                }
                else if (t.def == DefenseDefOf.RavenGas_Aphrodisiac)
                {
                    // 增加催情 Hediff
                    HealthUtility.AdjustSeverity(__instance, DefenseDefOf.RavenHediff_AphrodisiacEffect, 0.1f); // 每次+10%
                }
            }
        }
    }
}