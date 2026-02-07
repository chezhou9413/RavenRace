using System;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Collections.Generic;
using RavenRace.Features.Bloodline; // 确保你引用了这个命名空间，如果你的CompBloodline在这里

namespace RavenRace.Compat.MoeLotl
{
    [StaticConstructorOnStartup]
    public static class MoeLotlCompatUtility
    {
        public static Type CompCultivationType;
        public static Type CompAxolotlEnergyType;
        public static Type ITabCultivationType;
        public static Type CompUseAxolotlThingsType;
        public static Type CompUseEffectEnergyThingType;
        public static Type CompUseEffectBookType;
        public static Type WorkGiverReadBookType;
        public static Type MoeLotlSkillBookType;

        public static HediffDef LotlQiHediffDef;
        public static bool IsMoeLotlActive { get; private set; }

        // 【核心修正1】缓存萌螈原版的CompProperties
        private static CompProperties AxolotlEnergyCompProps;

        static MoeLotlCompatUtility()
        {
            try
            {
                ThingDef axolotlRace = DefDatabase<ThingDef>.GetNamedSilentFail("Axolotl");
                IsMoeLotlActive = (axolotlRace != null);

                if (!IsMoeLotlActive) return;

                CompCultivationType = AccessTools.TypeByName("Axolotl.Comp_Cultivation");
                CompAxolotlEnergyType = AccessTools.TypeByName("Axolotl.CompAxolotlEnergy");
                ITabCultivationType = AccessTools.TypeByName("Axolotl.ITab_MoeLotl_Cultivation");
                CompUseAxolotlThingsType = AccessTools.TypeByName("Axolotl.CompUseAxolotlThingsWithLabel");
                CompUseEffectEnergyThingType = AccessTools.TypeByName("Axolotl.ThingComp_UseAxolotlEnergyThing");
                CompUseEffectBookType = AccessTools.TypeByName("Axolotl.CompUseEffectGiveBookHediff");
                WorkGiverReadBookType = AccessTools.TypeByName("Axolotl.WorkGiver_ReadMoeLotlQiSkillBook");
                MoeLotlSkillBookType = AccessTools.TypeByName("Axolotl.MoeLotlSkillBook");

                LotlQiHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Axolotl_LotlQi");

                // 【核心修正2】在启动时，从萌螈的ThingDef中找到并缓存正确的CompProperties
                if (axolotlRace != null && CompAxolotlEnergyType != null)
                {
                    AxolotlEnergyCompProps = axolotlRace.comps.FirstOrDefault(p => p.compClass == CompAxolotlEnergyType);
                    if (AxolotlEnergyCompProps == null)
                    {
                        Log.Warning("[RavenRace] MoeLotl mod detected, but could not find CompProperties for 'CompAxolotlEnergy'. Drawing will likely fail.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Failed to initialize MoeLotl compatibility: {ex}");
                IsMoeLotlActive = false;
            }
        }

        public static void GrantCultivationAbility(Pawn pawn)
        {
            if (!IsMoeLotlActive || pawn == null) return;

            try
            {
                // 1. 处理修炼组件 (Comp_Cultivation)
                if (CompCultivationType != null && !pawn.AllComps.Any(c => c.GetType() == CompCultivationType))
                {
                    ThingComp newComp = (ThingComp)Activator.CreateInstance(CompCultivationType);
                    newComp.parent = pawn;
                    MethodInfo mPostMake = AccessTools.Method(CompCultivationType, "PostMake");
                    mPostMake?.Invoke(newComp, null);
                    pawn.AllComps.Add(newComp);
                }

                // 2. 处理能量组件 (CompAxolotlEnergy)
                if (CompAxolotlEnergyType != null && !pawn.AllComps.Any(c => c.GetType() == CompAxolotlEnergyType))
                {
                    // 【核心修正3】检查我们是否成功缓存了props
                    if (AxolotlEnergyCompProps == null)
                    {
                        Log.ErrorOnce("[RavenRace] Cannot grant Axolotl Energy Comp because its properties were not found.", 847291);
                        return;
                    }

                    ThingComp newComp = (ThingComp)Activator.CreateInstance(CompAxolotlEnergyType);
                    newComp.parent = pawn;
                    // 【核心修正4】使用缓存的、完整的props来初始化组件！
                    newComp.Initialize(AxolotlEnergyCompProps);
                    pawn.AllComps.Add(newComp);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Error granting MoeLotl abilities to {pawn.LabelShort}: {ex}");
            }
        }

        public static bool HasMoeLotlBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            // 确保你已经将 Comp_Bloodline 更名为 CompBloodline
            var comp = pawn.TryGetComp<CompBloodline>();
            if (comp != null && comp.BloodlineComposition != null)
            {
                if (comp.BloodlineComposition.ContainsKey("Axolotl"))
                {
                    return comp.BloodlineComposition["Axolotl"] > 0f;
                }
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
            catch
            {
                return null;
            }
        }

        public static void LogCompatibilityStatus()
        {
            if (!IsMoeLotlActive)
            {
                Log.Message("[RavenRace] MoeLotl compatibility: INACTIVE (Mod not detected)");
                return;
            }
            Log.Message("[RavenRace] MoeLotl Status: ACTIVE.");
        }
    }
}