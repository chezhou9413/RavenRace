using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RavenRace.Compat.MoeLotl
{
    [HarmonyPatch]
    public static class Patch_EnergyGainPerSec
    {
        static bool Prepare()
        {
            return AccessTools.TypeByName("Axolotl.CompAxolotlEnergy") != null;
        }

        static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("Axolotl.CompAxolotlEnergy");
            if (type == null) return null;
            return AccessTools.PropertyGetter(type, "EnergyGainPerSec");
        }

        public static bool Prefix(object __instance, ref float __result)
        {
            if (RavenRaceMod.Settings.enableMoeLotlCompat && MoeLotlCompatUtility.IsMoeLotlActive)
            {
                    Pawn pawn = AccessTools.Property(__instance.GetType(), "GetPawn")?.GetValue(__instance) as Pawn;
                    if (pawn?.def?.defName != "Raven_Race") return true;
                if (MoeLotlCompatUtility.HasMoeLotlBloodline(pawn))
                {
                    float num = 0f;
                    bool pawnHaveHediff = (bool)(AccessTools.Property(__instance.GetType(), "PawnHaveHediff")?.GetValue(__instance) ?? false);
                    if (pawnHaveHediff)
                    {
                        int level = (int)(AccessTools.Property(__instance.GetType(), "Level")?.GetValue(__instance) ?? 0);
                        num += 0.05f * level;

                        Type compCultType = AccessTools.TypeByName("Axolotl.Comp_Cultivation");
                        if (compCultType != null)
                        {
                            var cultComp = pawn.AllComps.Find(c => c.GetType() == compCultType);
                            if (cultComp != null)
                            {
                                float offset = (float)(AccessTools.Property(compCultType, "LotlQiGainOffsets")?.GetValue(cultComp) ?? 0f);
                                num += offset;
                            }
                        }

                        Type hediffCompType = AccessTools.TypeByName("Axolotl.HediffComp_LotlQiGain");
                        if (hediffCompType != null)
                        {
                            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                            {
                                if (hediff is HediffWithComps hd)
                                {
                                    var comp = hd.comps?.Find(c => c.GetType() == hediffCompType);
                                    if (comp != null)
                                    {
                                        float offset = (float)(AccessTools.Property(hediffCompType, "GetTrueLotlQiGainOffset")?.GetValue(comp) ?? 0f);
                                        num += offset;
                                    }
                                }
                            }
                        }

                        float breathingLevel = Mathf.Clamp(pawn.health.capacities.GetLevel(PawnCapacityDefOf.Breathing), 0.1f, 2f);
                        num *= breathingLevel;
                    }

                    __result = Mathf.Max(0f, num);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_IsMoeLotl
    {
        static bool Prepare()
        {
            return AccessTools.TypeByName("Axolotl.AxolotlExtension") != null;
        }

        static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("Axolotl.AxolotlExtension");
            if (type == null) return null;
            return AccessTools.Method(type, "IsMoeLotl");
        }

        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (RavenRaceMod.Settings.enableMoeLotlCompat && MoeLotlCompatUtility.IsMoeLotlActive)
            {
                if (MoeLotlCompatUtility.HasMoeLotlBloodline(pawn))
                {
                    if (pawn?.def?.defName == "Raven_Race")
                        __result = true;
                }            
            }
            else
            {
                __result = false;
            }
        }

        public static void TryPatch(HarmonyLib.Harmony harmony)
        {
            Type targetType = AccessTools.TypeByName("Axolotl.AxolotlExtension");
            if (targetType == null)
            {
                Log.Message("[RavenRace] 萌螈Mod未加载，跳过 IsMoeLotl 手动补丁。");
                return;
            }

            MethodInfo targetMethod = AccessTools.Method(targetType, "IsMoeLotl");
            if (targetMethod == null)
            {
                Log.Warning("[RavenRace] 找到萌螈但未找到 IsMoeLotl 方法，补丁跳过。");
                return;
            }

            MethodInfo postfix = AccessTools.Method(typeof(Patch_IsMoeLotl), nameof(Postfix));
            harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfix));
            Log.Message("[RavenRace] IsMoeLotl 补丁注册成功！渡鸦族现在会被萌螈识别为合法种族。");
        }
    }

    [StaticConstructorOnStartup]
    public static class RavenRaceStartup
    {
        static RavenRaceStartup()
        {
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.ravenrace.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Patch_IsMoeLotl.TryPatch(harmony);
            Log.Message("[RavenRace] 萌螈兼容补丁全部注册完成。");
        }
    }
}