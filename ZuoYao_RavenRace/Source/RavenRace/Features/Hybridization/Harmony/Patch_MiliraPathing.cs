using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using RavenRace.Compat.Milira;

// 命名空间修正
namespace RavenRace.Features.Hybridization.Harmony
{
    [StaticConstructorOnStartup]
    public static class Patch_MiliraPathing
    {
        static Patch_MiliraPathing()
        {
            DoPatch();
        }

        private static void DoPatch()
        {
            try
            {
                Type targetType = AccessTools.TypeByName("Milira.Pathing.MiliraPathingGlobal");
                if (targetType == null) return;

                // CS0118 错误修正：使用完全限定名 HarmonyLib.Harmony
                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("ZuoYao.RavenRace.MiliraCompat");

                MethodInfo mGetFlightMode = AccessTools.Method(targetType, "GetFlightMode", new Type[] { typeof(Pawn) });
                if (mGetFlightMode != null)
                {
                    harmony.Patch(
                        original: mGetFlightMode,
                        prefix: new HarmonyMethod(typeof(Patch_MiliraPathing), nameof(GetFlightMode_Prefix))
                    );
                    Log.Message("[RavenRace] Successfully patched MiliraPathingGlobal.GetFlightMode for flight compatibility.");
                }
                else
                {
                    Log.Error("[RavenRace] Failed to find MiliraPathingGlobal.GetFlightMode. Flight compatibility may not work.");
                }

                MethodInfo mIsFlightActive = AccessTools.Method(targetType, "IsFlightActive", new Type[] { typeof(Pawn) });
                if (mIsFlightActive != null)
                {
                    harmony.Patch(
                        original: mIsFlightActive,
                        prefix: new HarmonyMethod(typeof(Patch_MiliraPathing), nameof(IsFlightActive_Prefix))
                    );
                    Log.Message("[RavenRace] Successfully patched MiliraPathingGlobal.IsFlightActive.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RavenRace] Failed to apply Milira compatibility patches: {ex}");
            }
        }

        [HarmonyPriority(Priority.High)]
        public static bool GetFlightMode_Prefix(Pawn pawn, ref object __result)
        {
            try
            {
                if (pawn == null || !pawn.Spawned) return true;
                var proxy = pawn.TryGetComp<CompFlightControl>();
                if (proxy != null)
                {
                    if (proxy.IsActuallyFlying()) __result = proxy.onlyForMove ? 1 : 2;
                    else __result = 0;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[RavenRace] Critical error in GetFlightMode_Prefix: {ex}", 9384712);
                __result = 0;
                return false;
            }
            return true;
        }

        [HarmonyPriority(Priority.High)]
        public static bool IsFlightActive_Prefix(Pawn pawn, ref bool __result)
        {
            try
            {
                if (pawn == null) return true;
                var proxy = pawn.TryGetComp<CompFlightControl>();
                if (proxy != null)
                {
                    __result = proxy.IsActuallyFlying();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[RavenRace] Critical error in IsFlightActive_Prefix: {ex}", 9384713);
                __result = false;
                return false;
            }
            return true;
        }
    }
}