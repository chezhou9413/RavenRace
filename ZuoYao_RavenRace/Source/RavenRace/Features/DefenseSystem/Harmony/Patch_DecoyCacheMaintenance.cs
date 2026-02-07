using HarmonyLib;
using Verse;
using RimWorld;

namespace RavenRace.Features.DefenseSystem.Harmony
{
    /// <summary>
    /// 自动维护诱饵缓存的补丁
    /// </summary>
    [HarmonyPatch]
    public static class Patch_DecoyCacheMaintenance
    {
        // 假人的 ThingDef
        // 注意：Building_TurretGun 的 SpawnSetup/DeSpawn 最终会调用基类方法

        [HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]
        [HarmonyPostfix]
        public static void SpawnSetup_Postfix(Building __instance)
        {
            if (__instance.def == RavenDefOf.RavenDecoy_Dummy)
            {
                RavenDecoyCache.Register(__instance);
            }
        }

        [HarmonyPatch(typeof(Building), nameof(Building.DeSpawn))]
        [HarmonyPrefix]
        public static void DeSpawn_Prefix(Building __instance)
        {
            if (__instance.def == RavenDefOf.RavenDecoy_Dummy)
            {
                RavenDecoyCache.Deregister(__instance);
            }
        }
    }
}