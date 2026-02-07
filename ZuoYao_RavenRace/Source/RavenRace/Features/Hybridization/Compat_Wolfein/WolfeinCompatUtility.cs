using System;
using System.Linq;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;
using HarmonyLib;

namespace RavenRace.Compat.Wolfein
{
    [StaticConstructorOnStartup]
    public static class WolfeinCompatUtility
    {
        public static bool IsWolfeinActive { get; private set; }
        public static ThingDef WolfeinRaceDef { get; private set; }

        private static Type WolfeinStrengthCompType;
        // 【核心修正】缓存沃芬原版的CompProperties实例
        private static CompProperties WolfeinStrengthCompProps;

        static WolfeinCompatUtility()
        {
            WolfeinRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("Wolfein_Race");
            IsWolfeinActive = (WolfeinRaceDef != null);

            if (IsWolfeinActive)
            {
                WolfeinStrengthCompType = AccessTools.TypeByName("Wolfein.CompWolfeinStrength");
                if (WolfeinStrengthCompType != null)
                {
                    // 【核心修正】在启动时，从Wolfein_Race的ThingDef中找到并缓存正确的CompProperties
                    WolfeinStrengthCompProps = WolfeinRaceDef.comps.FirstOrDefault(p => p.compClass == WolfeinStrengthCompType);

                    if (WolfeinStrengthCompProps == null)
                    {
                        Log.Warning("[RavenRace] Wolfein mod detected, but could not find CompProperties for 'Wolfein.CompWolfeinStrength'. Compatibility is disabled.");
                        IsWolfeinActive = false;
                    }
                }
                else
                {
                    Log.Warning("[RavenRace] Wolfein mod detected, but could not find 'Wolfein.CompWolfeinStrength' component. Compatibility is disabled.");
                    IsWolfeinActive = false;
                }
            }
        }

        public static void HandleWolfeinBloodline(Pawn pawn, bool hasBloodline)
        {
            // 【核心修正】添加对WolfeinStrengthCompProps的非空检查
            if (!IsWolfeinActive || pawn == null || WolfeinStrengthCompType == null || WolfeinStrengthCompProps == null) return;

            bool hasComp = pawn.AllComps.Any(c => c.GetType() == WolfeinStrengthCompType);

            if (hasBloodline && !hasComp)
            {
                try
                {
                    ThingComp newComp = (ThingComp)Activator.CreateInstance(WolfeinStrengthCompType);
                    newComp.parent = pawn;
                    pawn.AllComps.Add(newComp);

                    // 【核心修正】使用我们缓存的、完整的CompProperties实例来初始化组件
                    newComp.Initialize(WolfeinStrengthCompProps);

                    if (pawn.Spawned) newComp.PostSpawnSetup(false);
                }
                catch (Exception ex)
                {
                    Log.Error($"[RavenRace] Failed to dynamically add Wolfein strength component to {pawn.LabelShort}: {ex}");
                }
            }
            else if (!hasBloodline && hasComp)
            {
                pawn.AllComps.RemoveAll(c => c.GetType() == WolfeinStrengthCompType);
            }
        }

        public static bool HasWolfeinBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("Wolfein_Race") &&
                   comp.BloodlineComposition["Wolfein_Race"] > 0f;
        }
    }
}