using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace.Features.Bloodline
{
    public static class BloodlineManager
    {
        private static Dictionary<ThingDef, BloodlineDef> raceToBloodlineCache;

        // 特殊常量 Key
        public const string MECHANIOD_BLOODLINE_KEY = "Bloodline_Mechanoid";

        public static void InitializePawnBloodline(Pawn pawn, CompBloodline comp)
        {
            if (pawn == null || comp == null) return;

            // 1. 检查是否为机械族 (优先于 Def 查找)
            if (pawn.RaceProps.IsMechanoid)
            {
                comp.BloodlineComposition.Clear();
                comp.BloodlineComposition.Add(MECHANIOD_BLOODLINE_KEY, 1.0f);
                comp.GoldenCrowConcentration = 0f;
                return;
            }

            // 2. 原有逻辑
            BloodlineDef bloodline = GetBloodlineDef(pawn.def);

            if (bloodline != null)
            {
                comp.BloodlineComposition.Clear();
                comp.BloodlineComposition.Add(pawn.def.defName, 1.0f);

                if (bloodline.isGoldenCrowSource)
                {
                    comp.GoldenCrowConcentration = Rand.Range(0.01f, 0.10f);
                }
                else
                {
                    comp.GoldenCrowConcentration = 0f;
                }

                RavenModUtility.LogVerbose($"Initialized known bloodline for {pawn.ThingID}: {bloodline.labelShort}");
            }
            else
            {
                comp.BloodlineComposition.Clear();
                comp.BloodlineComposition.Add(pawn.def.defName, 1.0f);
                comp.GoldenCrowConcentration = 0f;
            }
        }

        public static BloodlineDef GetBloodlineDef(ThingDef raceDef)
        {
            if (raceToBloodlineCache == null)
            {
                RebuildCache();
            }

            if (raceDef != null && raceToBloodlineCache.TryGetValue(raceDef, out BloodlineDef def))
            {
                return def;
            }
            return null;
        }

        private static void RebuildCache()
        {
            raceToBloodlineCache = new Dictionary<ThingDef, BloodlineDef>();
            List<BloodlineDef> allDefs = DefDatabase<BloodlineDef>.AllDefsListForReading;

            foreach (var def in allDefs)
            {
                if (def.raceDef != null && !raceToBloodlineCache.ContainsKey(def.raceDef))
                {
                    raceToBloodlineCache.Add(def.raceDef, def);
                }
            }
        }
    }
}