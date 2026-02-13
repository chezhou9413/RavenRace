using UnityEngine;
using Verse;

namespace RavenRace.Settings
{
    public static class Settings_Reproduction
    {
        public static void Draw(Listing_Standard listing)
        {
            var s = RavenRaceMod.Settings;
            listing.Label("RavenRace_Settings_ReproductionDesc".Translate());
            listing.GapLine();

            // 孵化
            listing.Label("RavenRace_Settings_BaseHatchingDays".Translate() + ": " + s.baseHatchingDays.ToString("0.0") + " Days");
            s.baseHatchingDays = listing.Slider(s.baseHatchingDays, 1f, 60f);

            listing.Gap();

            // 强制交配
            listing.Label("RavenRace_Settings_ForceLovinCooldownDays".Translate() + ": " + s.forceLovinCooldownDays.ToString("0.0") + " Days");
            s.forceLovinCooldownDays = listing.Slider(s.forceLovinCooldownDays, 0f, 15f);

            listing.CheckboxLabeled("RavenRace_Settings_EnableForceLovinPregnancy".Translate(), ref s.enableForceLovinPregnancy);
            if (s.enableForceLovinPregnancy)
            {
                listing.Label("RavenRace_Settings_ForcedLovinPregnancyRate".Translate() + ": " + s.forcedLovinPregnancyRate.ToStringPercent());
                s.forcedLovinPregnancyRate = listing.Slider(s.forcedLovinPregnancyRate, 0f, 1f);
                listing.CheckboxLabeled("RavenRace_Settings_IgnoreFertilityForPregnancy".Translate(), ref s.ignoreFertilityForPregnancy, "RavenRace_Settings_IgnoreFertilityForPregnancyDesc".Translate());
            }

            listing.CheckboxLabeled("RavenRace_Settings_EnableSameSexForceLovin".Translate(), ref s.enableSameSexForceLovin, "RavenRace_Settings_EnableSameSexForceLovinDesc".Translate());

            listing.CheckboxLabeled("启用机械体强制交配", ref s.enableMechanoidLovin, "允许对机械族单位使用强制求爱。机械族无法怀孕，但若渡鸦作为母体可产下带有机械血脉的后代。");

            listing.Gap();
            listing.Label("RavenRace_Settings_PrisonerInteractions".Translate());
            listing.Label("RavenRace_Settings_ForceLovinResistanceReduction".Translate() + ": " + s.forceLovinResistanceReduction.ToString("0.0"));
            s.forceLovinResistanceReduction = listing.Slider(s.forceLovinResistanceReduction, 0f, 20f);

            listing.Label("RavenRace_Settings_ForceLovinWillReduction".Translate() + ": " + s.forceLovinWillReduction.ToString("0.0"));
            s.forceLovinWillReduction = listing.Slider(s.forceLovinWillReduction, 0f, 20f);

            listing.Label("RavenRace_Settings_ForceLovinCertaintyReduction".Translate() + ": " + s.forceLovinCertaintyReduction.ToStringPercent());
            s.forceLovinCertaintyReduction = listing.Slider(s.forceLovinCertaintyReduction, 0f, 1f);

            listing.GapLine();
            listing.CheckboxLabeled("RavenRace_Settings_EnableMalePregnancyEgg".Translate(), ref s.enableMalePregnancyEgg, "RavenRace_Settings_EnableMalePregnancyEggDesc".Translate());
            listing.CheckboxLabeled("RavenRace_Settings_ForceRavenDescendant".Translate(), ref s.forceRavenDescendant, "RavenRace_Settings_ForceRavenDescendantDesc".Translate());
            listing.CheckboxLabeled("RavenRace_Settings_EnableEggProjectileMode".Translate(), ref s.enableEggProjectileMode, "RavenRace_Settings_EnableEggProjectileModeDesc".Translate());


            listing.GapLine();
            listing.Label("RavenRace_Settings_SpiritBeads".Translate());
            listing.CheckboxLabeled("RavenRace_Settings_EnableGrandClimax".Translate(), ref s.enableGrandClimax, "RavenRace_Settings_EnableGrandClimaxDesc".Translate());

            listing.GapLine();
            listing.Label("RavenRace_Settings_MasturbatorCup".Translate());
            listing.CheckboxLabeled("RavenRace_Settings_EnableDimensionalSex".Translate(), ref s.enableDimensionalSex, "RavenRace_Settings_EnableDimensionalSexDesc".Translate());

            listing.GapLine();
            listing.Label("灵卵温养设置");
            listing.Label($"完全温养所需时间: {s.spiritEggWarmthDays:F1} 天");

            s.spiritEggWarmthDays = listing.Slider(s.spiritEggWarmthDays, 0.01f, 5f);
            listing.Label("将灵卵保存在体内可进行温养，完美温养的灵卵在摇篮中孵化时会获得速度加成。");

            // [新增] RJW 兼容设置部分
            listing.GapLine();
            listing.Label("RJW 兼容设置");
            // 只有在RJW激活时才显示
            if (ModsConfig.IsActive("rim.job.world"))
            {
                listing.CheckboxLabeled("启用渡鸦怀孕兼容模式", ref s.rjwRavenPregnancyCompat, "开启后，使用“强制求爱”技能会立即触发渡鸦灵卵怀孕，RJW性爱过程仅作为表现。关闭则完全由RJW决定是否怀孕。");
            }
            else
            {
                listing.Label("未检测到RJW，相关设置已隐藏。");
            }
        }
    }
}