using System;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Collections.Generic;
using RavenRace.Features.Bloodline;
using System.Collections;

namespace RavenRace.Compat.MoeLotl
{
    /// <summary>
    /// 萌螈兼容性工具类 - 最终完整版
    /// 包含所有字段定义、血脉检查逻辑以及存档修复逻辑。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MoeLotlCompatUtility
    {
        // =============================================================
        // 1. 静态类型引用
        // =============================================================
        public static Type CompCultivationType;
        public static Type CompAxolotlEnergyType;
        public static Type ITabCultivationType;
        public static Type CompUseAxolotlThingsType;
        public static Type CompUseEffectEnergyThingType;
        public static Type CompUseEffectBookType;
        public static Type WorkGiverReadBookType;
        public static Type MoeLotlSkillBookType;

        public static Type MoeLotlQiSkillType;
        public static Type MoeLotlQiSkillDefType;

        public static HediffDef LotlQiHediffDef;
        public static HediffDef MoeLotlBloodlineHediff;
        public static bool IsMoeLotlActive { get; private set; }

        // =============================================================
        // 2. 存档修复专用缓存
        // =============================================================
        public static CompProperties CachedCultivationProps;
        public static CompProperties CachedEnergyProps;

        static MoeLotlCompatUtility()
        {
            try
            {
                ThingDef axolotlRace = DefDatabase<ThingDef>.GetNamedSilentFail("Axolotl");
                if (axolotlRace == null)
                {
                    IsMoeLotlActive = false;
                    return;
                }

                CompCultivationType = AccessTools.TypeByName("Axolotl.Comp_Cultivation");
                CompAxolotlEnergyType = AccessTools.TypeByName("Axolotl.CompAxolotlEnergy");
                ITabCultivationType = AccessTools.TypeByName("Axolotl.ITab_MoeLotl_Cultivation");
                CompUseAxolotlThingsType = AccessTools.TypeByName("Axolotl.CompUseAxolotlThingsWithLabel");
                CompUseEffectEnergyThingType = AccessTools.TypeByName("Axolotl.ThingComp_UseAxolotlEnergyThing");
                CompUseEffectBookType = AccessTools.TypeByName("Axolotl.CompUseEffectGiveBookHediff");
                WorkGiverReadBookType = AccessTools.TypeByName("Axolotl.WorkGiver_ReadMoeLotlQiSkillBook");
                MoeLotlSkillBookType = AccessTools.TypeByName("Axolotl.MoeLotlSkillBook");

                MoeLotlQiSkillType = AccessTools.TypeByName("Axolotl.MoeLotlQiSkill");
                MoeLotlQiSkillDefType = AccessTools.TypeByName("Axolotl.MoeLotlQiSkillDef");

                LotlQiHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Axolotl_LotlQi");
                MoeLotlBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MoeLotlBloodline");

                IsMoeLotlActive = (CompCultivationType != null && CompAxolotlEnergyType != null);

                if (!IsMoeLotlActive) return;

                if (CompCultivationType != null)
                    CachedCultivationProps = axolotlRace.comps.FirstOrDefault(p => p.compClass == CompCultivationType);

                if (CompAxolotlEnergyType != null)
                    CachedEnergyProps = axolotlRace.comps.FirstOrDefault(p => p.compClass == CompAxolotlEnergyType);

                if (CachedCultivationProps == null && CompCultivationType != null)
                {
                    Type propType = AccessTools.TypeByName("Axolotl.CompProperties_Cultivation");
                    if (propType != null)
                        CachedCultivationProps = (CompProperties)Activator.CreateInstance(propType);
                    else
                        CachedCultivationProps = new CompProperties { compClass = CompCultivationType };
                }

                if (CachedEnergyProps == null && CompAxolotlEnergyType != null)
                {
                    Type propType = AccessTools.TypeByName("Axolotl.CompProperties_AxolotlEnergy");
                    if (propType != null)
                        CachedEnergyProps = (CompProperties)Activator.CreateInstance(propType);
                    else
                        CachedEnergyProps = new CompProperties { compClass = CompAxolotlEnergyType };
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Failed to initialize MoeLotl compatibility: {ex}");
                IsMoeLotlActive = false;
            }
        }

        public static void HandleMoeLotlBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || MoeLotlBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(MoeLotlBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(MoeLotlBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(MoeLotlBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }

        public static void GrantCultivationAbility(Pawn pawn)
        {
            if (!IsMoeLotlActive || pawn == null) return;
            try
            {
                EnsureCompExistsAndInitialize(pawn, CompCultivationType, CachedCultivationProps);
                EnsureCompExistsAndInitialize(pawn, CompAxolotlEnergyType, CachedEnergyProps);
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Error processing MoeLotl abilities for {pawn.LabelShort}: {ex}");
            }
        }

        private static void EnsureCompExistsAndInitialize(Pawn pawn, Type compType, CompProperties props)
        {
            if (compType == null || props == null) return;
            if (pawn.AllComps.Any(c => c.GetType() == compType)) return;

            try
            {
                ThingComp newComp = (ThingComp)Activator.CreateInstance(compType);
                newComp.parent = pawn;
                newComp.Initialize(props);

                MethodInfo mPostMake = AccessTools.Method(compType, "PostMake");
                mPostMake?.Invoke(newComp, null);

                pawn.AllComps.Add(newComp);
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Failed to create and add MoeLotl component '{compType.Name}' to {pawn.LabelShort}. Error: {ex}");
            }
        }

        // =============================================================
        // [恢复] 供外部补丁和组件调用的 API
        // =============================================================
        public static bool HasMoeLotlBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            var comp = pawn.TryGetComp<CompBloodline>();
            return BloodlineUtility.HasBloodline(comp, "Axolotl");
        }

        public static ThingDef GetTargetReadBook(Pawn pawn)
        {
            if (!IsMoeLotlActive || CompCultivationType == null) return null;
            try
            {
                var comp = pawn.AllComps.FirstOrDefault(c => c.GetType() == CompCultivationType);
                if (comp == null) return null;
                FieldInfo field = AccessTools.Field(CompCultivationType, "TargetReadBook");
                if (field != null) return (ThingDef)field.GetValue(comp);
                return null;
            }
            catch { return null; }
        }

        public static void ExposeCultivationData(Pawn pawn)
        {
            if (!IsMoeLotlActive || pawn == null) return;

            ThingComp cultComp = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                cultComp = pawn.AllComps.FirstOrDefault(c => c.GetType() == CompCultivationType);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                EnsureCompExistsAndInitialize(pawn, CompCultivationType, CachedCultivationProps);
                cultComp = pawn.AllComps.FirstOrDefault(c => c.GetType() == CompCultivationType);
                EnsureCompExistsAndInitialize(pawn, CompAxolotlEnergyType, CachedEnergyProps);
            }
            else
            {
                cultComp = pawn.AllComps.FirstOrDefault(c => c.GetType() == CompCultivationType);
            }

            if (cultComp == null) return;

            try
            {
                Traverse trav = Traverse.Create(cultComp);

                var allLearnedSkills = trav.Field("AllLearnedSkills").GetValue();
                LookEx_List(ref allLearnedSkills, "Axolotl.MoeLotlQiSkill", "Raven_MoeLotl_Skills", LookMode.Deep, Array.Empty<object>());
                trav.Field("AllLearnedSkills").SetValue(allLearnedSkills);

                var targetReadBook = trav.Field("TargetReadBook").GetValue<ThingDef>();
                Scribe_Defs.Look(ref targetReadBook, "Raven_MoeLotl_TargetBook");
                trav.Field("TargetReadBook").SetValue(targetReadBook);

                var skillLearningProgress = trav.Field("KV_SkillLearningProgress").GetValue();
                LookEx_Dict(ref skillLearningProgress, "Axolotl.MoeLotlQiSkillDef", typeof(int), "Raven_MoeLotl_Progress", LookMode.Def, LookMode.Value);
                trav.Field("KV_SkillLearningProgress").SetValue(skillLearningProgress);

                var skillLearningMax = trav.Field("KV_SkillLearningMax").GetValue();
                LookEx_Dict(ref skillLearningMax, "Axolotl.MoeLotlQiSkillDef", typeof(int), "Raven_MoeLotl_Max", LookMode.Def, LookMode.Value);
                trav.Field("KV_SkillLearningMax").SetValue(skillLearningMax);

                var skillInstallList = trav.Field("SkillInstallList").GetValue();
                LookEx_List(ref skillInstallList, "Axolotl.MoeLotlQiSkillDef", "Raven_MoeLotl_InstallList", LookMode.Def, Array.Empty<object>());
                trav.Field("SkillInstallList").SetValue(skillInstallList);

                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    ReinitializeSkills(pawn, cultComp, allLearnedSkills as IList);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Error manually scribing MoeLotl data for {pawn.LabelShort}: {ex}");
            }
        }

        private static void ReinitializeSkills(Pawn pawn, ThingComp cultComp, IList skillsList)
        {
            if (skillsList == null) return;

            for (int i = skillsList.Count - 1; i >= 0; i--)
            {
                if (skillsList[i] == null) skillsList.RemoveAt(i);
            }

            foreach (var skill in skillsList)
            {
                if (skill == null) continue;
                Traverse st = Traverse.Create(skill);

                st.Field("pawn").SetValue(pawn);

                try
                {
                    st.Method("InitializeComps").GetValue();
                    st.Method("UpdateCurrentStatModifier").GetValue();
                    st.Method("Notify_LevelChange").GetValue();
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RavenRace] Failed to re-init skill {skill}: {ex.Message}");
                }
            }
        }

        private static void LookEx_List(ref object list, string typeName, string label, LookMode lookMode, object[] ctorArgs)
        {
            Type itemType = AccessTools.TypeByName(typeName);
            if (itemType == null) return;

            var method = typeof(Scribe_Collections).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == "Look" && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 4 && m.GetParameters()[3].ParameterType == typeof(object[]))
                .MakeGenericMethod(itemType);

            object[] args = new object[] { list, label, lookMode, ctorArgs ?? new object[0] };
            method.Invoke(null, args);
            list = args[0];
        }

        private static void LookEx_Dict(ref object dict, string keyTypeName, Type valueType, string label, LookMode keyLookMode, LookMode valueLookMode)
        {
            Type keyType = AccessTools.TypeByName(keyTypeName);
            if (keyType == null) return;

            var method = typeof(Scribe_Collections).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == "Look" && m.GetGenericArguments().Length == 2 && m.GetParameters().Length == 4 && m.GetParameters()[2].ParameterType == typeof(LookMode))
                .MakeGenericMethod(keyType, valueType);

            object[] args = new object[] { dict, label, keyLookMode, valueLookMode };
            method.Invoke(null, args);
            dict = args[0];
        }
    }
}