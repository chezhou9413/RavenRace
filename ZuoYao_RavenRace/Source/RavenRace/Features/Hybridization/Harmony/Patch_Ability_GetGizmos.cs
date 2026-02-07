using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using RavenRace.Compat.Epona; // 引用

// 命名空间修正
namespace RavenRace.Features.Hybridization.Harmony
{
    [StaticConstructorOnStartup]
    public static class Patch_Ability_GetGizmos
    {
        static Patch_Ability_GetGizmos()
        {
            // CS0118 错误修正：使用完全限定名 HarmonyLib.Harmony
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("ZuoYao.RavenRace.EponaCompat");
            var original = AccessTools.Method(typeof(Ability), "GetGizmos");
            var postfix = AccessTools.Method(typeof(Patch_Ability_GetGizmos), nameof(Postfix));

            if (original != null && postfix != null)
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                // if (Prefs.DevMode) Log.Message("[RavenRace] Epona Ability Gizmo Patch Applied.");
            }
        }

        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Ability __instance)
        {
            foreach (var gizmo in __result)
            {
                if (gizmo is Command_Ability cmd && __instance.def.defName == "Raven_Ability_MuGirlCharge")
                {
                    Pawn pawn = __instance.pawn;

                    if (RavenRaceMod.Settings.enableMuGirlCompat &&
                        EponaCompatUtility.IsEponaActive &&
                        EponaCompatUtility.HasEponaBloodline(pawn))
                    {
                        cmd.defaultLabel = "RavenRace_AbilityLabel_EnhancedCharge".Translate();
                        cmd.defaultDesc = "RavenRace_AbilityDesc_EnhancedCharge".Translate();
                    }
                }
                yield return gizmo;
            }
        }
    }
}