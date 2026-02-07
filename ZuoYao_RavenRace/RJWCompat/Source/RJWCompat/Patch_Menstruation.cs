using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using RavenRace.Features.Reproduction; // [核心修复] 引用新的命名空间

namespace RavenRace.RJWCompat
{
    [HarmonyPatch]
    public static class Patch_Menstruation_Implant
    {
        // 使用字符串名称进行延迟绑定，防止RJW未加载时直接崩溃
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("RJW_Menstruation.HediffComp_Menstruation:Implant");
        }

        [HarmonyPrefix]
        public static bool Prefix(HediffComp __instance)
        {
            // 如果反射初始化失败，直接跳过
            if (!RjwReflection.IsInitialized) return true;

            // 通过反射安全地获取字段值
            var eggsList = RjwReflection.Menstruation_eggs_Field.GetValue(__instance) as IList;
            if (eggsList == null || eggsList.Count == 0) return true;

            object fertilizedEgg = null;
            foreach (var egg in eggsList)
            {
                bool isFertilized = (bool)RjwReflection.Egg_fertilized_Field.GetValue(egg);
                object fertilizer = RjwReflection.Egg_fertilizer_Field.GetValue(egg);
                if (isFertilized && fertilizer != null)
                {
                    fertilizedEgg = egg;
                    break;
                }
            }

            if (fertilizedEgg == null) return true;

            Pawn mother = __instance.Pawn;
            Pawn father = RjwReflection.Egg_fertilizer_Field.GetValue(fertilizedEgg) as Pawn;

            // --- 判定是否应该产卵 (逻辑与主模组一致) ---
            bool shouldLayEgg = false;

            // 1. 获取设置
            var settings = RavenRaceMod.Settings;

            // 2. 男性生子(蛋)逻辑
            if (settings.enableMalePregnancyEgg && mother.gender == Gender.Male)
            {
                if (mother.def.defName == "Raven_Race" || (father != null && father.def.defName == "Raven_Race"))
                {
                    shouldLayEgg = true;
                }
            }
            // 3. 渡鸦母亲
            else if (mother.def.defName == "Raven_Race")
            {
                shouldLayEgg = true;
            }
            // 4. 父系决定逻辑
            else if (settings.ravenFatherDeterminesEgg && father != null && father.def.defName == "Raven_Race")
            {
                shouldLayEgg = true;
            }

            if (!shouldLayEgg) return true;

            try
            {
                Log.Message("[RavenRace RJWCompat] Intercepting RJW pregnancy, creating Spirit Egg hediff...");

                bool success;
                List<GeneDef> inheritedGenesList = PregnancyUtility.GetInheritedGenes(father, mother, out success);
                if (!success)
                {
                    // 仅作为警告，不阻止流程
                    // Log.Warning($"[RavenRace RJWCompat] Failed to get inherited genes.");
                }
                GeneSet inheritedGeneSet = new GeneSet();
                foreach (var g in inheritedGenesList) inheritedGeneSet.AddGene(g);

                if (father != null && GeneUtility.SameHeritableXenotype(father, mother))
                {
                    inheritedGeneSet.SetNameDirect(father.genes?.xenotypeName);
                }

                // [核心修复] 使用新的类名 HediffRavenPregnancy，并使用 DefOf 获取 Def
                var hediff = HediffMaker.MakeHediff(RavenDefOf.Raven_Hediff_RavenPregnancy, mother) as HediffRavenPregnancy;

                if (hediff != null)
                {
                    hediff.Initialize(father, inheritedGeneSet, settings.forceRavenDescendant);
                    mother.health.AddHediff(hediff);
                    mother.Drawer?.renderer?.SetAllGraphicsDirty();
                }

                // 清空原版月经Mod的卵子列表，防止双重怀孕
                eggsList.Clear();

                Messages.Message($"{mother.LabelShort} has conceived a spirit egg (RJW-compatible).", mother, MessageTypeDefOf.PositiveEvent);

                // 返回 false 阻止原方法执行（即阻止RJW的原版怀孕逻辑）
                return false;
            }
            catch (Exception e)
            {
                Log.Error($"[RavenRace RJWCompat] Error during spirit egg conception interception: {e}");
                return true;
            }
        }
    }
}