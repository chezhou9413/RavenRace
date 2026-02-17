using System;
using System.Collections.Generic;
using RavenRace.Settings;
using RavenRace.HarmonyPatches; // 确保引用了补丁命名空间
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace
{
    public class RavenRaceSettings : ModSettings
    {
        // ===================================================
        // 1. 基础与调试 (Base & Debug)
        // ===================================================
        public bool enableDebugMode = false;
        public bool enableVerboseLogging = false;

        // [新增] 余烬之血概率设置 (归类于 Base)
        public float emberBloodDeathChance = 0.3f;
        public float emberBloodBerserkChance = 0.3f;

        // [新增] 金羽设置
        public float featherDropMoodThreshold = 0.05f; // 默认 5% 悲伤/ 95% 快乐
        public int featherDropCheckInterval = 250;     // 默认 TickRare (不可调，因为方法固定)
        public float featherDropChance = 0.02f;        // 默认 2%
        public float featherCooldownDays = 60f;        // 默认 60天

        public bool showFeatherCooldown = false;

        // [新增] 大渡鸦设置
        public bool enableGreatRavenShiny = true;
        public float greatRavenSearchDays = 3.0f; // 默认3天一次

        // ===================================================
        // 2. 血脉系统 (Bloodline)
        // ===================================================
        public float bloodlineInheritanceStrength = 1.0f;
        public float goldenCrowDecayRate = 0.1f;
        public bool enableBloodlineMutations = true;

        // ===================================================
        // 3. 育生祭坛 (Soul Altar)
        // ===================================================
        public float baseHatchingDays = 15f;
        public int altarEffectRadius = 5;
        public bool enableAltarVisualEffects = true;

        // ===================================================
        // 4. 繁殖与互动 (Reproduction)
        // ===================================================
        public bool enableForceLovinPregnancy = true;
        public float forcedLovinPregnancyRate = 0.05f;
        public float forceLovinCooldownDays = 1.0f;

        public bool ravenFatherDeterminesEgg = false;
        public bool enableMalePregnancyEgg = false;
        public bool ignoreFertilityForPregnancy = false;
        public bool enableSameSexForceLovin = true;

        // [新增] 机械族交配开关
        public bool enableMechanoidLovin = false;

        public bool forceRavenDescendant = true;
        public bool enableEggProjectileMode = false;

        // 囚犯/奴隶互动参数
        public float forceLovinResistanceReduction = 2f;
        public float forceLovinWillReduction = 0.1f;
        public float forceLovinCertaintyReduction = 0.1f;
        public float forceLovinInstantRecruitChance = 0.1f;
        public float forceLovinBreakLoyaltyChance = 0.05f;

        public float spiritEggWarmthDays = 3f; // 灵卵温养所需天数

        public bool enableGrandClimax = false; //灵卵拉珠相关，默认关闭。

        // [新增] 飞机杯设置
        public bool enableDimensionalSex = false; // 默认关闭，作为彩蛋


        // [新增] RJW兼容开关
        public bool rjwRavenPregnancyCompat = true;

        // ===================================================
        // 5. 杂交兼容 (Hybridization)
        // ===================================================


        public bool enableMiliraCompat = true; // 米莉拉 (Milira)
        public bool enableMoeLotlCompat = true; // 萌螈 (MoeLotl)
        public bool enableKoelimeBloodline = true; // 珂莉姆 (Koelime)
        public bool enableMuGirlCompat = true; // 雪牛娘 (MuGirl)
        public bool enableMuffaloPrank = false;  // 雪牛(Muffalo)彩蛋开关
        public bool enableWolfeinCompat = true; // 沃芬 (Wolfein)
        public bool enableDragonianCompat = true; // 龙人 (Dragonian)
        public bool enableMoyoCompat = true; // [Phase 3.6.8] 莫约 (Moyo)                                          
        public bool enableEponaCompat = true;// Epona Compat
        public bool enableTailinCompat = true;// 泰临 (Tailin)
        public bool enableCinderCompat = true; // 烟烬 (Cinder)

        // ===================================================
        // 6. 扶桑组织 (Fusang)
        // ===================================================
        public float fusangCommCooldownDays = 3f;
        public float reinforcementDelayHours = 6f;
        public float tradePriceModifier = 1.0f;

        // [新增] 商队交易冷却 (归类于 Fusang)
        public float tradeCaravanCooldownDays = 3.0f;

        // ===================================================
        // 7. 间谍系统 (Espionage)
        // ===================================================
        public float missionSuccessBonus = 0f;
        public bool enableManualInfiltration = true;
        public float missionFailureCooldownDays = 7f;
        public float missionCostMultiplier = 1.0f;
        public float missionDurationMultiplier = 1.0f;

        // ===================================================
        // 8. 防卫系统 (Defense)
        // ===================================================
        public bool enableDefenseSystemDebug = false;
        public float trapDamageMultiplier = 1.0f;
        public bool friendlyFireSafe = true;

        // 升墙陷阱大小 (5, 7, 9, 11, 13)
        public int risingWallSize = 5;

        // ===================================================
        // 9. 建筑与娱乐 (Buildings & Entertainment)
        // ===================================================
        public float incenseJoyAmount = 0.05f;
        public float incenseForceLovinChance = 0.05f;
        public int incenseCheckInterval = 250;

        // --- 电视机功能设置 ---
        public float avMatingChance = 0.02f; // 默认 2% 概率
        public float avJoyWeightMultiplier = 1.0f; // 娱乐权重倍率
        public bool avDisableTolerance = false;    // 禁用娱乐耐受/厌倦

        // ===================================================
        // UI 状态与逻辑
        // ===================================================
        private enum SettingsTab
        {
            Base,
            Bloodline,
            Altar,
            Reproduction,
            Hybridization,
            Fusang,
            Operator,
            Espionage,
            Defense,
            Buildings,
        }

        private SettingsTab currentTab = SettingsTab.Base;
        private Vector2 scrollPosition = Vector2.zero;

        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect tabRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
            DrawTabs(tabRect);
            Rect contentRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, 1500f);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            switch (currentTab)
            {
                case SettingsTab.Base: Settings_Base.Draw(listing); break;
                case SettingsTab.Bloodline: Settings_Bloodline.Draw(listing); break;
                case SettingsTab.Altar: Settings_Altar.Draw(listing); break;
                case SettingsTab.Reproduction: Settings_Reproduction.Draw(listing); break;
                case SettingsTab.Hybridization: Settings_Hybridization.Draw(listing); break;
                case SettingsTab.Fusang: Settings_Fusang.Draw(listing); break;
                case SettingsTab.Operator: Settings_Operator.Draw(listing); break;
                case SettingsTab.Espionage: Settings_Espionage.Draw(listing); break;
                case SettingsTab.Defense: Settings_Defense.Draw(listing); break;
                case SettingsTab.Buildings: Settings_Buildings.Draw(listing); break;
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawTabs(Rect rect)
        {
            Array tabs = Enum.GetValues(typeof(SettingsTab));
            float tabWidth = rect.width / tabs.Length;

            for (int i = 0; i < tabs.Length; i++)
            {
                SettingsTab tab = (SettingsTab)tabs.GetValue(i);
                Rect tRect = new Rect(rect.x + tabWidth * i, rect.y, tabWidth, rect.height);
                if (currentTab == tab) GUI.color = Color.yellow;
                if (Widgets.ButtonText(tRect, $"RavenRace_Settings_{tab}".Translate()))
                {
                    currentTab = tab;
                    scrollPosition = Vector2.zero;
                }
                GUI.color = Color.white;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref enableDebugMode, "enableDebugMode", false);
            Scribe_Values.Look(ref enableVerboseLogging, "enableVerboseLogging", false);
            Scribe_Values.Look(ref emberBloodDeathChance, "emberBloodDeathChance", 0.3f);
            Scribe_Values.Look(ref emberBloodBerserkChance, "emberBloodBerserkChance", 0.3f);
            Scribe_Values.Look(ref featherDropMoodThreshold, "featherDropMoodThreshold", 0.05f);
            Scribe_Values.Look(ref featherDropChance, "featherDropChance", 0.02f);
            Scribe_Values.Look(ref featherCooldownDays, "featherCooldownDays", 60f);
            Scribe_Values.Look(ref showFeatherCooldown, "showFeatherCooldown", false);

            // [新增] 大渡鸦设置 (归类于 Base)
            Scribe_Values.Look(ref enableGreatRavenShiny, "enableGreatRavenShiny", true);
            Scribe_Values.Look(ref greatRavenSearchDays, "greatRavenSearchDays", 3.0f);



            Scribe_Values.Look(ref bloodlineInheritanceStrength, "bloodlineInheritanceStrength", 1.0f);
            Scribe_Values.Look(ref goldenCrowDecayRate, "goldenCrowDecayRate", 0.1f);
            Scribe_Values.Look(ref enableBloodlineMutations, "enableBloodlineMutations", true);
            Scribe_Values.Look(ref baseHatchingDays, "baseHatchingDays", 15f);
            Scribe_Values.Look(ref altarEffectRadius, "altarEffectRadius", 5);
            Scribe_Values.Look(ref enableAltarVisualEffects, "enableAltarVisualEffects", true);
            Scribe_Values.Look(ref enableForceLovinPregnancy, "enableForceLovinPregnancy", true);
            Scribe_Values.Look(ref forcedLovinPregnancyRate, "forcedLovinPregnancyRate", 0.05f);
            Scribe_Values.Look(ref forceLovinCooldownDays, "forceLovinCooldownDays", 1.0f);
            Scribe_Values.Look(ref ravenFatherDeterminesEgg, "ravenFatherDeterminesEgg", false);
            Scribe_Values.Look(ref enableMalePregnancyEgg, "enableMalePregnancyEgg", false);
            Scribe_Values.Look(ref ignoreFertilityForPregnancy, "ignoreFertilityForPregnancy", false);
            Scribe_Values.Look(ref enableSameSexForceLovin, "enableSameSexForceLovin", true);
            Scribe_Values.Look(ref enableMechanoidLovin, "enableMechanoidLovin", false);             // 和机械族
            Scribe_Values.Look(ref forceRavenDescendant, "forceRavenDescendant", true);
            Scribe_Values.Look(ref enableEggProjectileMode, "enableEggProjectileMode", false);
            Scribe_Values.Look(ref forceLovinResistanceReduction, "forceLovinResistanceReduction", 2f);
            Scribe_Values.Look(ref forceLovinWillReduction, "forceLovinWillReduction", 0.1f);
            Scribe_Values.Look(ref forceLovinCertaintyReduction, "forceLovinCertaintyReduction", 0.1f);
            Scribe_Values.Look(ref forceLovinInstantRecruitChance, "forceLovinInstantRecruitChance", 0.1f);
            Scribe_Values.Look(ref forceLovinBreakLoyaltyChance, "forceLovinBreakLoyaltyChance", 0.05f);
            Scribe_Values.Look(ref spiritEggWarmthDays, "spiritEggWarmthDays", 3f);


            Scribe_Values.Look(ref enableGrandClimax, "enableGrandClimax", false);
            Scribe_Values.Look(ref enableDimensionalSex, "enableDimensionalSex", false);

            Scribe_Values.Look(ref rjwRavenPregnancyCompat, "rjwRavenPregnancyCompat", true); // [新增] 保存 RJW 兼容设置

            Scribe_Values.Look(ref enableMiliraCompat, "enableMiliraCompat", true);
            Scribe_Values.Look(ref enableMoeLotlCompat, "enableMoeLotlCompat", true);
            Scribe_Values.Look(ref enableKoelimeBloodline, "enableKoelimeBloodline", true);
            Scribe_Values.Look(ref enableMuGirlCompat, "enableMuGirlCompat", true);
            Scribe_Values.Look(ref enableMuffaloPrank, "enableMuffaloPrank", false);
            Scribe_Values.Look(ref enableWolfeinCompat, "enableWolfeinCompat", true);
            Scribe_Values.Look(ref enableDragonianCompat, "enableDragonianCompat", true);
            Scribe_Values.Look(ref enableMoyoCompat, "enableMoyoCompat", true);
            Scribe_Values.Look(ref enableEponaCompat, "enableEponaCompat", true);
            Scribe_Values.Look(ref enableTailinCompat, "enableTailinCompat", true);// 泰临 (Tailin)
            Scribe_Values.Look(ref enableCinderCompat, "enableCinderCompat", true);// 烟烬 (Cinder)



            Scribe_Values.Look(ref fusangCommCooldownDays, "fusangCommCooldownDays", 3f);
            Scribe_Values.Look(ref reinforcementDelayHours, "reinforcementDelayHours", 6f);
            Scribe_Values.Look(ref tradePriceModifier, "tradePriceModifier", 1.0f);
            Scribe_Values.Look(ref tradeCaravanCooldownDays, "tradeCaravanCooldownDays", 3.0f);
            Scribe_Values.Look(ref missionSuccessBonus, "missionSuccessBonus", 0f);
            Scribe_Values.Look(ref enableManualInfiltration, "enableManualInfiltration", true);
            Scribe_Values.Look(ref missionFailureCooldownDays, "missionFailureCooldownDays", 7f);
            Scribe_Values.Look(ref missionCostMultiplier, "missionCostMultiplier", 1.0f);
            Scribe_Values.Look(ref missionDurationMultiplier, "missionDurationMultiplier", 1.0f);
            Scribe_Values.Look(ref enableDefenseSystemDebug, "enableDefenseSystemDebug", false);
            Scribe_Values.Look(ref trapDamageMultiplier, "trapDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref friendlyFireSafe, "friendlyFireSafe", true);
            Scribe_Values.Look(ref risingWallSize, "risingWallSize", 5);
            Scribe_Values.Look(ref incenseJoyAmount, "incenseJoyAmount", 0.05f);
            Scribe_Values.Look(ref incenseForceLovinChance, "incenseForceLovinChance", 0.05f);
            Scribe_Values.Look(ref incenseCheckInterval, "incenseCheckInterval", 250);
        }

        public void OnSettingsChanged()
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                // [Change] RavenRace.HarmonyPatches -> RavenRace.Harmony, Patch_RavenWallColor -> RavenWallColorPatch
                RavenRace.Harmony.RavenWallColorPatch.ClearCache();

                foreach (Pawn p in PawnsFinder.AllMaps_FreeColonistsSpawned)
                {
                    p.Drawer?.renderer?.SetAllGraphicsDirty();
                }
            }
        }
    }
}