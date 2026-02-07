using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using RavenRace.Features.Hybridization.Harmony;

namespace RavenRace.Compat.MoeLotl
{
    [StaticConstructorOnStartup]
    public static class Patch_MoeLotl_Compat
    {
        private static WorkTypeDef readBookWorkType;

        // 【核心修正1】获取出问题的HediffComp类型
        private static Type hediffCompWaterTreatType;

        static Patch_MoeLotl_Compat()
        {
            if (!MoeLotlCompatUtility.IsMoeLotlActive) return;

            readBookWorkType = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Axolotl_ReadMoeLotlQiSkillBooks");
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("ZuoYao.RavenRace.MoeLotlCompat");

            // 【核心修正2】在启动时获取类型，以便后续打补丁
            hediffCompWaterTreatType = AccessTools.TypeByName("Axolotl.HediffCompWaterTreatToSelf");

            try
            {
                // ... (省略你已有的所有补丁注册代码，它们保持不变)
                var mIsMoeLotl = AccessTools.Method("Axolotl.AxolotlExtension:IsMoeLotl");
                if (mIsMoeLotl != null)
                    harmony.Patch(mIsMoeLotl, prefix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(IsMoeLotl_Prefix)));

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
                if (MoeLotlCompatUtility.CompCultivationType != null)
                {
                    var mPostExposeData = AccessTools.Method(MoeLotlCompatUtility.CompCultivationType, "PostExposeData");
                    if (mPostExposeData != null) harmony.Patch(mPostExposeData, prefix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(CompCultivation_PostExposeData_Prefix)));
                }

                Type contextType = AccessTools.TypeByName("RimWorld.FloatMenuContext");
                var mGetOptions = AccessTools.Method(typeof(FloatMenuMakerMap), "GetOptions", new Type[] { typeof(List<Pawn>), typeof(Vector3), contextType.MakeByRefType() });
                if (mGetOptions != null)
                {
                    harmony.Patch(mGetOptions, postfix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(FloatMenuMakerMap_Postfix)));
                }

                // 【核心修正3】在这里添加对HediffComp的补丁
                if (hediffCompWaterTreatType != null)
                {
                    var mCompPostTick = AccessTools.Method(hediffCompWaterTreatType, "CompPostTick");
                    if (mCompPostTick != null)
                    {
                        harmony.Patch(mCompPostTick, prefix: new HarmonyMethod(typeof(Patch_MoeLotl_Compat), nameof(HediffCompWaterTreat_CompPostTick_Prefix)));
                    }
                }

                ApplyMiscPatches(harmony);

                RavenModUtility.LogVerbose("[RavenRace] MoeLotl compatibility patches applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Error applying MoeLotl compatibility patches: {ex}");
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

        // ==========================================
        // 【核心修正4】添加新的Prefix补丁方法
        // ==========================================
        public static bool HediffCompWaterTreat_CompPostTick_Prefix(HediffComp __instance)
        {
            // 通过__instance.Pawn获取当前Pawn
            Pawn pawn = __instance.Pawn;
            if (pawn != null && pawn.def.defName == "Raven_Race" && MoeLotlCompatUtility.HasMoeLotlBloodline(pawn))
            {
                try
                {
                    // 手动模拟萌螈的治疗逻辑
                    if (pawn.Map != null && pawn.Position.GetTerrain(pawn.Map).IsWater)
                    {
                        // 假设治疗逻辑是简单的每秒恢复一定量的伤口
                        // 这是一个安全的、基于原版API的实现，即使萌螈更新了也不会崩溃
                        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                        {
                            if (hediff is Hediff_Injury injury && injury.CanHealNaturally())
                            {
                                // 每秒治疗0.1，这里每60tick大约治疗0.1
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

                // 返回 false，跳过原版的、会导致我们崩溃的方法
                return false;
            }

            // 如果不是我们的混血渡鸦，则正常执行原版方法
            return true;
        }

        // --- 省略所有其他未修改的补丁方法，保持它们的原样 ---
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
                        if (RavenRaceMod.Settings.enableDebugMode)
                        {
                            string tName = targetBookDef?.defName ?? "NULL";
                            string cName = t.def.defName;
                            Log.Message($"[RavenRace] RightClick: Pawn={pawn.LabelShort}, TargetInComp={tName}, Clicked={cName}");
                        }

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

        public static void CompCultivation_PostExposeData_Prefix(ThingComp __instance)
        {
            if (__instance == null) return;
            var trav = Traverse.Create(__instance);
            InitializeField(trav, "AllLearnedSkills", "Axolotl.MoeLotlQiSkill");
            InitializeField(trav, "SkillInstallList", "Axolotl.MoeLotlQiSkillDef");
            InitializeDict(trav, "KV_SkillLearningProgress", "Axolotl.MoeLotlQiSkillDef", typeof(int));
            InitializeDict(trav, "KV_SkillLearningMax", "Axolotl.MoeLotlQiSkillDef", typeof(int));
        }

        private static void InitializeField(Traverse trav, string fieldName, string typeName)
        {
            if (trav.Field(fieldName).GetValue<object>() == null)
            {
                Type type = AccessTools.TypeByName(typeName);
                Type listType = typeof(List<>).MakeGenericType(type);
                trav.Field(fieldName).SetValue(Activator.CreateInstance(listType));
            }
        }

        private static void InitializeDict(Traverse trav, string fieldName, string keyTypeName, Type valType)
        {
            if (trav.Field(fieldName).GetValue<object>() == null)
            {
                Type keyType = AccessTools.TypeByName(keyTypeName);
                Type dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valType);
                trav.Field(fieldName).SetValue(Activator.CreateInstance(dictType));
            }
        }

        public static bool IsMoeLotl_Prefix(Pawn pawn, ref bool __result)
        {
            if (pawn == null) return true;
            if (pawn.def.defName == "Raven_Race")
            {
                if (RavenRaceMod.Settings.enableMoeLotlCompat && MoeLotlCompatUtility.HasMoeLotlBloodline(pawn))
                {
                    __result = true;
                    return false;
                }
            }
            return true;
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