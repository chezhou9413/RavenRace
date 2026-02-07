using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using RavenRace.Compat.MuGirl;

// 命名空间修正
namespace RavenRace.Features.Hybridization.Harmony
{
    [StaticConstructorOnStartup]
    public static class Patch_Ability_Enhanced
    {
        static Patch_Ability_Enhanced()
        {
            // CS0118 错误修正：使用完全限定名 HarmonyLib.Harmony
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("ZuoYao.RavenRace.EponaEnhanced");

            var originalGizmo = AccessTools.Method(typeof(Ability), "GetGizmos");
            var postfixGizmo = AccessTools.Method(typeof(Patch_Ability_Enhanced), nameof(Postfix_GetGizmos));
            if (originalGizmo != null) harmony.Patch(originalGizmo, postfix: new HarmonyMethod(postfixGizmo));

            var originalRange = AccessTools.PropertyGetter(typeof(Verb), "Range");
            if (originalRange != null)
            {
                var postfixRange = AccessTools.Method(typeof(Patch_Ability_Enhanced), nameof(Postfix_GetRange));
                harmony.Patch(originalRange, postfix: new HarmonyMethod(postfixRange));
            }
        }

        public static IEnumerable<Gizmo> Postfix_GetGizmos(IEnumerable<Gizmo> __result, Ability __instance)
        {
            foreach (var gizmo in __result)
            {
                if (gizmo is Command_Ability cmd && __instance.def.defName == "Raven_Ability_MuGirlCharge")
                {
                    Pawn pawn = __instance.pawn;
                    if (RavenRaceMod.Settings.enableMuGirlCompat &&
                        Compat.Epona.EponaCompatUtility.IsEponaActive &&
                        Compat.Epona.EponaCompatUtility.HasEponaBloodline(pawn))
                    {
                        cmd.defaultLabel = "RavenRace_AbilityLabel_EnhancedCharge".Translate();
                        cmd.defaultDesc = "RavenRace_AbilityDesc_EnhancedCharge".Translate();
                    }
                }
                yield return gizmo;
            }
        }

        public static void Postfix_GetRange(Verb __instance, ref float __result)
        {
            if (__instance is Verb_CastAbility castVerb && castVerb.ability != null)
            {
                if (castVerb.ability.def.defName == "Raven_Ability_MuGirlCharge")
                {
                    Pawn pawn = castVerb.CasterPawn;
                    if (pawn != null &&
                        RavenRaceMod.Settings.enableMuGirlCompat &&
                        Compat.Epona.EponaCompatUtility.IsEponaActive &&
                        Compat.Epona.EponaCompatUtility.HasEponaBloodline(pawn))
                    {
                        __result = 35.9f;
                    }
                }
            }
        }
    }
}