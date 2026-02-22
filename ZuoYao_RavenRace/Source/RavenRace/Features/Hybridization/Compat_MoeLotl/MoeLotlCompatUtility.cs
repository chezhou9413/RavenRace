using System;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Collections.Generic;
using RavenRace.Features.Bloodline;
using System.Collections; // 必须引用

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

        // 反射萌螈的数据类型
        public static Type MoeLotlQiSkillType;
        public static Type MoeLotlQiSkillDefType;

        public static HediffDef LotlQiHediffDef;
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
                // 获取萌螈种族定义
                ThingDef axolotlRace = DefDatabase<ThingDef>.GetNamedSilentFail("Axolotl");
                if (axolotlRace == null)
                {
                    IsMoeLotlActive = false;
                    return;
                }

                // 反射类型
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

                // 获取 Hediff
                LotlQiHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Axolotl_LotlQi");

                // 只要核心组件存在，就视为激活
                IsMoeLotlActive = (CompCultivationType != null && CompAxolotlEnergyType != null);

                if (!IsMoeLotlActive) return;

                // 缓存 Props
                if (CompCultivationType != null)
                    CachedCultivationProps = axolotlRace.comps.FirstOrDefault(p => p.compClass == CompCultivationType);

                if (CompAxolotlEnergyType != null)
                    CachedEnergyProps = axolotlRace.comps.FirstOrDefault(p => p.compClass == CompAxolotlEnergyType);

                // 兜底逻辑
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

        // =============================================================
        // 3. 核心功能方法
        // =============================================================

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

        public static bool HasMoeLotlBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            var comp = pawn.TryGetComp<CompBloodline>();
            if (comp != null && comp.BloodlineComposition != null)
            {
                return comp.BloodlineComposition.ContainsKey("Axolotl") &&
                       comp.BloodlineComposition["Axolotl"] > 0f;
            }
            return false;
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

        // =============================================================
        // 4. [关键] 手动存档接管逻辑
        // =============================================================

        /// <summary>
        /// 在 CompBloodline.PostExposeData 中调用。
        /// 手动将 Comp_Cultivation 的数据保存到 CompBloodline 的存档流中。
        /// </summary>
        public static void ExposeCultivationData(Pawn pawn)
        {
            if (!IsMoeLotlActive || pawn == null) return;

            // 1. 获取或创建组件（确保 Loading 时组件存在）
            ThingComp cultComp = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // 保存时，只获取已存在的
                cultComp = pawn.AllComps.FirstOrDefault(c => c.GetType() == CompCultivationType);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // 加载时，必须强制创建，否则 Scribe 没地方写数据
                EnsureCompExistsAndInitialize(pawn, CompCultivationType, CachedCultivationProps);
                cultComp = pawn.AllComps.FirstOrDefault(c => c.GetType() == CompCultivationType);

                // 同样保证 Energy 组件存在
                EnsureCompExistsAndInitialize(pawn, CompAxolotlEnergyType, CachedEnergyProps);
            }
            else
            {
                // PostLoadInit 等其他阶段，尝试获取
                cultComp = pawn.AllComps.FirstOrDefault(c => c.GetType() == CompCultivationType);
            }

            if (cultComp == null) return; // 如果还没血脉或没组件，就不存

            // 2. 利用反射进行 Scribe
            // 只要我们进入了 Scribe_Deep 节点，这里的 Look 就会写入 Bloodline 组件下的子节点
            try
            {
                Traverse trav = Traverse.Create(cultComp);

                // A. AllLearnedSkills (List<MoeLotlQiSkill>)
                var allLearnedSkills = trav.Field("AllLearnedSkills").GetValue();
                LookEx_List(ref allLearnedSkills, "Axolotl.MoeLotlQiSkill", "Raven_MoeLotl_Skills", LookMode.Deep, Array.Empty<object>());
                trav.Field("AllLearnedSkills").SetValue(allLearnedSkills);

                // B. TargetReadBook (ThingDef)
                var targetReadBook = trav.Field("TargetReadBook").GetValue<ThingDef>();
                Scribe_Defs.Look(ref targetReadBook, "Raven_MoeLotl_TargetBook");
                trav.Field("TargetReadBook").SetValue(targetReadBook);

                // C. KV_SkillLearningProgress
                var skillLearningProgress = trav.Field("KV_SkillLearningProgress").GetValue();
                LookEx_Dict(ref skillLearningProgress, "Axolotl.MoeLotlQiSkillDef", typeof(int), "Raven_MoeLotl_Progress", LookMode.Def, LookMode.Value);
                trav.Field("KV_SkillLearningProgress").SetValue(skillLearningProgress);

                // D. KV_SkillLearningMax
                var skillLearningMax = trav.Field("KV_SkillLearningMax").GetValue();
                LookEx_Dict(ref skillLearningMax, "Axolotl.MoeLotlQiSkillDef", typeof(int), "Raven_MoeLotl_Max", LookMode.Def, LookMode.Value);
                trav.Field("KV_SkillLearningMax").SetValue(skillLearningMax);

                // E. SkillInstallList
                var skillInstallList = trav.Field("SkillInstallList").GetValue();
                LookEx_List(ref skillInstallList, "Axolotl.MoeLotlQiSkillDef", "Raven_MoeLotl_InstallList", LookMode.Def, Array.Empty<object>());
                trav.Field("SkillInstallList").SetValue(skillInstallList);

                // 3. 后处理初始化 (模拟萌螈原版的 PostExposeData 逻辑)
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

            // 清理空元素
            for (int i = skillsList.Count - 1; i >= 0; i--)
            {
                if (skillsList[i] == null) skillsList.RemoveAt(i);
            }

            foreach (var skill in skillsList)
            {
                if (skill == null) continue;
                Traverse st = Traverse.Create(skill);

                // 1. 核心修复：强制重置 pawn 引用
                // 萌螈原版用 Scribe_References 保存，但在嵌套Scribe中可能不稳定
                st.Field("pawn").SetValue(pawn);

                // 2. 调用萌螈原版初始化逻辑
                // public virtual void Notify_LevelChange() -> Calls UpdateCurrentStatModifier -> InitializeComps
                // 我们直接调用反编译代码中看到的流程
                try
                {
                    st.Method("InitializeComps").GetValue(); // 实例化技能组件
                    st.Method("UpdateCurrentStatModifier").GetValue(); // 更新数值
                    st.Method("Notify_LevelChange").GetValue(); // 触发更新
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RavenRace] Failed to re-init skill {skill}: {ex.Message}");
                }
            }
        }

        // =============================================================
        // 5. 反射 Scribe 辅助方法 (复用之前的逻辑)
        // =============================================================
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