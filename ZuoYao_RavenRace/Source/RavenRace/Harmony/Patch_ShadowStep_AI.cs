using HarmonyLib;
using Verse;
using RimWorld;
using Verse.AI;
using RavenRace.Features.UniqueEquipment.ShadowCloak;

namespace RavenRace.HarmonyPatches
{
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class Patch_ShadowStep_AI
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            // 如果有影步 Buff，禁止 AI 自动索敌
            if (pawn.health != null &&
                ShadowCloakDefOf.Raven_Hediff_ShadowStep != null &&
                pawn.health.hediffSet.HasHediff(ShadowCloakDefOf.Raven_Hediff_ShadowStep))
            {
                __result = null;
                return false; // 拦截原方法
            }
            return true;
        }
    }
}