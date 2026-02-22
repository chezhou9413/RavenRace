using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RavenRace.Compat.MoeLotl
{
    /// <summary>
    /// 萌螈模组功能性兼容补丁集合。
    /// 主要负责拦截萌螈模组的各种功能性检查，确保我们的混血渡鸦能够被正确识别和处理。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_MoeLotl_Compat
    {
        private static WorkTypeDef readBookWorkType;
        private static Type hediffCompWaterTreatType;

        static Patch_MoeLotl_Compat()
        {
            if (!MoeLotlCompatUtility.IsMoeLotlActive) return;

            readBookWorkType = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Axolotl_ReadMoeLotlQiSkillBooks");
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("ZuoYao.RavenRace.MoeLotlCompat");

            hediffCompWaterTreatType = AccessTools.TypeByName("Axolotl.HediffCompWaterTreatToSelf");

            try
            {
                // ==============================================================
                // 1. [核心功能] 全局补丁 IsMoeLotl()
                // ==============================================================
                var mIsMoeLotl = AccessTools.Method("Axolotl.AxolotlExtension:IsMoeLotl");
                if (mIsMoeLotl != null)
                {
                    harmony.Patch(mIsMoeLotl, postfix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(IsMoeLotl_Postfix)));
                }

                // ==============================================================
                // 2. [核心修复] 拦截萌螈组件的原版存档逻辑
                // 防止出现 "Id already used" 红字。因为数据已经由 CompBloodline 手动接管。
                // ==============================================================
                if (MoeLotlCompatUtility.CompCultivationType != null)
                {
                    var mPostExposeData = AccessTools.Method(MoeLotlCompatUtility.CompCultivationType, "PostExposeData");
                    if (mPostExposeData != null)
                    {
                        harmony.Patch(mPostExposeData, prefix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(CompCultivation_PostExposeData_Prefix)));
                    }

                    // [稳定性修复] 吞掉 GetStatOffset 的内部异常，防止世界格 Pawn Tick 时刷红字
                    var mGetStatOffset = AccessTools.Method(MoeLotlCompatUtility.CompCultivationType, "GetStatOffset");
                    if (mGetStatOffset != null)
                    {
                        harmony.Patch(mGetStatOffset, finalizer: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(CompCultivation_GetStatOffset_Finalizer)));
                    }
                }

                // ==============================================================
                // 3. 其他功能性补丁
                // ==============================================================
                if (MoeLotlCompatUtility.CompUseEffectEnergyThingType != null)
                {
                    var mCanBeUsedBy = AccessTools.Method(MoeLotlCompatUtility.CompUseEffectEnergyThingType, "CanBeUsedBy");
                    if (mCanBeUsedBy != null) harmony.Patch(mCanBeUsedBy, postfix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(CanBeUsedBy_Postfix)));
                }
                if (MoeLotlCompatUtility.CompUseEffectBookType != null)
                {
                    var mCanBeUsedByBook = AccessTools.Method(MoeLotlCompatUtility.CompUseEffectBookType, "CanBeUsedBy");
                    if (mCanBeUsedByBook != null) harmony.Patch(mCanBeUsedByBook, postfix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(CanBeUsedBy_Postfix)));
                }
                if (MoeLotlCompatUtility.CompAxolotlEnergyType != null)
                {
                    var pEnergyGain = AccessTools.PropertyGetter(MoeLotlCompatUtility.CompAxolotlEnergyType, "EnergyGainPerSec");
                    if (pEnergyGain != null)
                    {
                        harmony.Patch(pEnergyGain, prefix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(EnergyGainPerSec_Prefix)));
                    }
                }
                if (MoeLotlCompatUtility.MoeLotlSkillBookType != null)
                {
                    var mGetFloatMenuOptions = AccessTools.Method(MoeLotlCompatUtility.MoeLotlSkillBookType, "GetFloatMenuOptions");
                    if (mGetFloatMenuOptions != null)
                    {
                        harmony.Patch(mGetFloatMenuOptions, prefix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(SkillBook_GetFloatMenuOptions_Prefix)));
                    }
                }

                Type contextType = AccessTools.TypeByName("RimWorld.FloatMenuContext");
                var mGetOptions = AccessTools.Method(typeof(FloatMenuMakerMap), "GetOptions", new Type[] { typeof(List<Pawn>), typeof(Vector3), contextType.MakeByRefType() });
                if (mGetOptions != null)
                {
                    harmony.Patch(mGetOptions, postfix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(FloatMenuMakerMap_Postfix)));
                }

                if (hediffCompWaterTreatType != null)
                {
                    var mCompPostTick = AccessTools.Method(hediffCompWaterTreatType, "CompPostTick");
                    if (mCompPostTick != null)
                    {
                        harmony.Patch(mCompPostTick, prefix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(HediffCompWaterTreat_CompPostTick_Prefix)));
                    }
                }

                ApplyMiscPatches(harmony);

                // RavenModUtility.LogVerbose("[RavenRace] MoeLotl compatibility patches applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Error applying MoeLotl compatibility patches: {ex}");
            }
        }

        // ==============================================================
        // 补丁实现
        // ==============================================================

        /// <summary>
        /// [拦截补丁] 如果是渡鸦族，禁止运行 Comp_Cultivation.PostExposeData。
        /// 这里的逻辑完全由 MoeLotlCompatUtility.ExposeCultivationData 手动处理。
        /// </summary>
        public static bool CompCultivation_PostExposeData_Prefix(ThingComp __instance)
        {
            Pawn pawn = __instance.parent as Pawn;
            if (pawn != null && pawn.def == RavenDefOf.Raven_Race)
            {
                return false; // 拦截原版，防止双重注册 ID 导致的红字
            }
            return true; // 其他种族正常执行
        }

        public static void IsMoeLotl_Postfix(Pawn pawn, ref bool __result)
        {
            if (__result || pawn == null) return;

            if (pawn.def == RavenDefOf.Raven_Race &&
                RavenRaceMod.Settings.enableMoeLotlCompat &&
                MoeLotlCompatUtility.HasMoeLotlBloodline(pawn))
            {
                __result = true;
            }
        }

        private static void ApplyMiscPatches(HarmonyLib.Harmony harmony)
        {
            if (readBookWorkType != null)
            {
                var mWorkTypeIsDisabled = AccessTools.Method(typeof(Pawn), "WorkTypeIsDisabled");
                if (mWorkTypeIsDisabled != null) harmony.Patch(mWorkTypeIsDisabled, postfix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(WorkTypeIsDisabled_Postfix)) { priority = Priority.Last });
            }
            if (MoeLotlCompatUtility.MoeLotlSkillBookType != null)
            {
                var mCanReadBook = AccessTools.Method(MoeLotlCompatUtility.MoeLotlSkillBookType, "CanReadBook");
                if (mCanReadBook != null) harmony.Patch(mCanReadBook, postfix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(CanReadBook_Postfix)) { priority = Priority.Last });
            }
            var mAddHediff = AccessTools.Method(typeof(Pawn_HealthTracker), "AddHediff", new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) });
            if (mAddHediff != null) harmony.Patch(mAddHediff, prefix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(AddHediff_Prefix)));
            if (MoeLotlCompatUtility.CompAxolotlEnergyType != null)
            {
                var mValidateTarget = AccessTools.Method(MoeLotlCompatUtility.CompAxolotlEnergyType, "ValidateTarget");
                if (mValidateTarget != null) harmony.Patch(mValidateTarget, postfix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(ValidateTarget_Postfix)));
                var mGetGizmos = AccessTools.Method(MoeLotlCompatUtility.CompAxolotlEnergyType, "CompGetGizmosExtra");
                if (mGetGizmos != null) harmony.Patch(mGetGizmos, finalizer: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(GetGizmos_Finalizer)));
            }
        }

        public static bool HediffCompWaterTreat_CompPostTick_Prefix(HediffComp __instance)
        {
            Pawn pawn = __instance.Pawn;
            if (pawn != null && pawn.def.defName == "Raven_Race" && MoeLotlCompatUtility.HasMoeLotlBloodline(pawn))
            {
                try
                {
                    if (pawn.Map != null && pawn.Position.GetTerrain(pawn.Map).IsWater)
                    {
                        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                        {
                            if (hediff is Hediff_Injury injury && injury.CanHealNaturally())
                            {
                                if (__instance.parent.ageTicks % 60 == 0)
                                {
                                    injury.Heal(0.1f);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce($"[RavenRace] Error in manual water treat logic for {pawn.LabelShort}: {ex}", 19283746);
                }
                return false;
            }
            return true;
        }

        #region Unchanged Methods
        public static void FloatMenuMakerMap_Postfix(List<Pawn> selectedPawns, Vector3 clickPos, ref List<FloatMenuOption> __result)
        {
            try
            {
                if (!MoeLotlCompatUtility.IsMoeLotlActive) return;
                if (selectedPawns == null || selectedPawns.Count != 1) return;
                Pawn pawn = selectedPawns[0];
                if (pawn.def.defName != "Raven_Race" || !MoeLotlCompatUtility.HasMoeLotlBloodline(pawn)) return;

                IntVec3 c = IntVec3.FromVector3(clickPos);
                if (!c.InBounds(pawn.Map)) return;

                List<Thing> things = c.GetThingList(pawn.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t.GetType() == MoeLotlCompatUtility.MoeLotlSkillBookType)
                    {
                        string label = "RavenRace_ForceReadBook".Translate(t.LabelShort);
                        if (!pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly))
                        {
                            __result.Add(new FloatMenuOption(label + " (" + "NoPath".Translate() + ")", null));
                            continue;
                        }

                        ThingDef targetBookDef = MoeLotlCompatUtility.GetTargetReadBook(pawn);

                        if (targetBookDef != t.def)
                        {
                            __result.Add(new FloatMenuOption(label + " (" + "RavenRace_WrongBook".Translate() + ")", null));
                            continue;
                        }

                        Action action = delegate
                        {
                            JobDef jobDef = DefDatabase<JobDef>.GetNamed("Axolotl_ReadMoeLotlQiSkillBooks");
                            Job job = JobMaker.MakeJob(jobDef, t);
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        };

                        __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action), pawn, t));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[RavenRace] Error in FloatMenuMakerMap Postfix: {ex}", 7384122);
            }
        }

        public static bool EnergyGainPerSec_Prefix(ThingComp __instance, ref float __result)
        {
            Pawn pawn = __instance.parent as Pawn;
            if (pawn != null && pawn.def.defName == "Raven_Race" && MoeLotlCompatUtility.HasMoeLotlBloodline(pawn))
            {
                float level = 0f;
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MoeLotlCompatUtility.LotlQiHediffDef);
                if (hediff != null) level = hediff.Severity;
                float gainValue = 0.05f * level;
                var compCult = pawn.AllComps.FirstOrDefault(c => c.GetType() == MoeLotlCompatUtility.CompCultivationType);
                if (compCult != null)
                {
                    var pOffset = AccessTools.Property(MoeLotlCompatUtility.CompCultivationType, "LotlQiGainOffsets");
                    if (pOffset != null)
                    {
                        gainValue += (float)pOffset.GetValue(compCult, null);
                    }
                }
                float breathing = Mathf.Clamp(pawn.health.capacities.GetLevel(PawnCapacityDefOf.Breathing), 0.1f, 2.0f);
                gainValue *= breathing;
                gainValue *= 1.0f;
                __result = Mathf.Max(0, gainValue);
                return false;
            }
            return true;
        }

        public static bool SkillBook_GetFloatMenuOptions_Prefix(Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            if (selPawn != null && selPawn.def.defName == "Raven_Race")
            {
                __result = Enumerable.Empty<FloatMenuOption>();
                return false;
            }
            return true;
        }

        public static void CanBeUsedBy_Postfix(Pawn user, ref AcceptanceReport __result)
        {
            if (!__result.Accepted)
            {
                if (user != null && user.def.defName == "Raven_Race" && MoeLotlCompatUtility.HasMoeLotlBloodline(user))
                {
                    __result = true;
                }
            }
        }

        public static Exception CompCultivation_GetStatOffset_Finalizer(
    Exception __exception, ref float __result)
        {
            if (__exception != null)
            {
                __result = 0f;
                return null;
            }
            return null;
        }

        public static Exception GetGizmos_Finalizer(Exception __exception) { return null; }

        public static void AddHediff_Prefix(Pawn_HealthTracker __instance, Hediff hediff, ref BodyPartRecord part)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn != null && pawn.def.defName == "Raven_Race" && MoeLotlCompatUtility.HasMoeLotlBloodline(pawn))
            {
                if (hediff.def.defName.StartsWith("Axolotl_") && part == null)
                    part = pawn.RaceProps.body.corePart;
            }
        }

        public static void WorkTypeIsDisabled_Postfix(ref bool __result, Pawn __instance, WorkTypeDef w)
        {
            if (__result && w == readBookWorkType)
            {
                if (__instance != null && __instance.def.defName == "Raven_Race" && MoeLotlCompatUtility.HasMoeLotlBloodline(__instance)) __result = false;
            }
        }

        public static void CanReadBook_Postfix(object[] __args, ref bool __result)
        {
            if (__result) return;
            Pawn reader = __args[1] as Pawn;
            if (reader != null && reader.def.defName == "Raven_Race" && MoeLotlCompatUtility.HasMoeLotlBloodline(reader))
            {
                if (!reader.health.capacities.CapableOf(PawnCapacityDefOf.Sight)) return;
                __result = true;
                __args[2] = null;
            }
        }

        public static void ValidateTarget_Postfix(LocalTargetInfo target, ref bool __result)
        {
            if (__result) return;
            Pawn p = target.Pawn;
            if (p != null && p.def.defName == "Raven_Race" && MoeLotlCompatUtility.HasMoeLotlBloodline(p))
            {
                if (MoeLotlCompatUtility.LotlQiHediffDef != null && p.health.hediffSet.HasHediff(MoeLotlCompatUtility.LotlQiHediffDef))
                    __result = true;
            }
        }
        #endregion
    }
}