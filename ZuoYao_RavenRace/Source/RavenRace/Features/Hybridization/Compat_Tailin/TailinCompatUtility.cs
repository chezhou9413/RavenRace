using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Linq;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Tailin
{
    [StaticConstructorOnStartup]
    public static class TailinCompatUtility
    {
        public static bool IsTailinActive { get; private set; }

        public static ThingDef TailinRaceDef { get; private set; }
        public static HediffDef TailinBloodlineHediff { get; private set; }

        static TailinCompatUtility()
        {
            try
            {
                TailinRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("TailinRace");
                IsTailinActive = (TailinRaceDef != null);
                TailinBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Tailin_CombatInstinct");

                if (IsTailinActive)
                {
                    Log.Message($"[RavenRace] 泰临补丁运行成功: ");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] 泰临补丁运行失败: {ex}");
                IsTailinActive = false;
            }
        }

        public static bool HasTailinBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            // 【核心修正】将 "Tailin" 改为正确的种族 DefName "TailinRace"
            return comp.BloodlineComposition.ContainsKey("TailinRace") && comp.BloodlineComposition["TailinRace"] > 0f;
        }

        public static void HandleTailinBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || TailinBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(TailinBloodlineHediff);

            // 如果应该有但没有，则添加
            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(TailinBloodlineHediff);
            }
            // 如果不应该有但有，则移除
            else if (!hasBloodline && hasHediff)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TailinBloodlineHediff);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }
}