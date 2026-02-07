using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace
{
    // 拦截精神崩溃
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public static class Patch_MentalBreak_Feather
    {
        [HarmonyPostfix]
        public static void Postfix(MentalStateHandler __instance, bool __result, Pawn ___pawn)
        {
            // 只有崩溃成功才触发
            if (__result && ___pawn != null)
            {
                var comp = ___pawn.TryGetComp<CompRavenFeatherDrop>();
                comp?.Notify_MentalBreak();
            }
        }
    }

    // 拦截倒地
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_Downed_Feather
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn)
        {
            // 倒地后触发
            if (___pawn != null && ___pawn.Downed)
            {
                var comp = ___pawn.TryGetComp<CompRavenFeatherDrop>();
                // [Fixed] 调用新方法名
                comp?.HandleDownedEvent();
            }
        }
    }
}
