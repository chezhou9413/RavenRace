using System;
using System.Collections.Generic;
using RavenRace.Settings;
using RavenRace.Core.Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace
{
    /// <summary>
    /// 模组设置类，存储所有玩家可配置的选项。
    /// 继承自ModSettings，RimWorld会自动处理其存档和读档。
    /// </summary>
    public class RavenRaceSettings : ModSettings
    {
        // ===================================================
        // 1. 基础与调试 (Base & Debug)
        // ===================================================
        public bool enableDebugMode = false;            // 【调试】是否启用开发者调试模式。会解锁一些调试用的Gizmo和日志。
        public bool enableVerboseLogging = false;       // 【调试】是否启用详细日志输出。用于追踪问题，普通玩家建议关闭。
        public bool enableMemeSounds = false;           // 【彩蛋】是否启用整蛊音效。
        public float emberBloodDeathChance = 0.3f;      // 【血脉】注射“余烬之血”后直接死亡的概率。
        public float emberBloodBerserkChance = 0.3f;    // 【血脉】注射“余烬之血”后精神崩溃（狂暴）的概率。
        public float featherDropMoodThreshold = 0.05f; // 【物品】“折翼金羽”在情绪极高或极低时掉落的阈值。
        public float featherDropChance = 0.02f;         // 【物品】“折翼金羽”在满足情绪条件时，每次检测的掉落几率。
        public float featherCooldownDays = 60f;         // 【物品】“折翼金羽”掉落后的冷却时间（游戏天）。
        public bool showFeatherCooldown = false;        // 【UI】是否在Pawn的检查面板显示金羽的冷却状态。
        public bool enableGreatRavenShiny = true;       // 【生物】是否启用渡鸦大统领的“寻找亮闪闪”功能。
        public float greatRavenSearchDays = 3.0f;       // 【生物】渡鸦大统领寻找宝物的间隔时间（游戏天）。
        public float greatRavenGoldChance = 0.95f;      // 【生物】渡鸦大统领寻宝时，发现纯金的概率。
        public float greatRavenItemChance = 0.04f;      // 【生物】渡鸦大统领寻宝时，发现金制品的概率。
        public float greatRavenCubeChance = 0.01f;      // 【生物】渡鸦大统领寻宝时，发现“齁金魔方”的概率。

        public float servitudeInteractionChance = 0.1f;    // 【侍奉】侍奉者主动发起互动的基础概率。
        public float servitudeCooldownMultiplier = 1.0f;   // 【侍奉】侍奉者所有互动冷却时间的全局倍率。

        // ===================================================
        // 2. 血脉系统 (Bloodline)
        // ===================================================
        public float bloodlineInheritanceStrength = 1.0f; // 【血脉】杂交后代继承父母血脉的强度。
        public float goldenCrowDecayRate = 0.1f;          // 【血脉】金乌血脉浓度的自然衰减速率。
        public bool enableBloodlineMutations = true;      // 【血脉】是否允许在杂交时发生低概率的血脉突变。

        public float purificationSuccessChance = 0.5f;    // 【纯化】纯化仪式成功的概率
        public int purificationRitualDurationTicks = 5000; // 【纯化】纯化仪式的耗时(Ticks)

        // ===================================================
        // 3. 育生祭坛 (Soul Altar)
        // ===================================================
        public float baseHatchingDays = 15f;            // 【祭坛】灵卵在原始摇篮中的基础孵化天数。
        public int altarEffectRadius = 5;               // 【祭坛】祭坛核心扫描周围组件的半径。
        public bool enableAltarVisualEffects = true;    // 【祭坛】是否启用祭坛的视觉特效。

        // ===================================================
        // 4. 繁殖与互动 (Reproduction)
        // ===================================================
        public bool enableForceLovinPregnancy = true;     // 【繁殖】是否允许“强制求爱”技能导致怀孕。
        public float forcedLovinPregnancyRate = 0.05f;    // 【繁殖】“强制求爱”成功后的基础怀孕率。
        public float forceLovinCooldownDays = 1.0f;       // 【繁殖】“强制求爱”技能的冷却时间（游戏天）。
        public bool ravenFatherDeterminesEgg = false;     // 【繁殖】是否由父亲的种族决定后代是否为卵生。
        public bool enableMalePregnancyEgg = false;       // 【繁殖】是否允许男性渡鸦族通过“强制求爱”怀孕并产下灵卵。
        public bool ignoreFertilityForPregnancy = false;  // 【繁殖】在计算“强制求爱”的怀孕率时，是否忽略双方的生育能力属性。
        public bool enableSameSexForceLovin = true;       // 【繁殖】是否允许对同性使用“强制求爱”技能。
        public bool enableMechanoidLovin = false;         // 【繁殖-彩蛋】是否允许对机械体使用“强制求爱”。
        public bool enableBuildingLovin = false;          // 【繁殖-彩蛋】是否允许对墙体使用“强制求爱”。
        public bool forceRavenDescendant = true;          // 【繁殖】是否强制让渡鸦族的杂交后代总是渡鸦族。
        public bool enableEggProjectileMode = false;      // 【繁殖-彩蛋】是否启用“灵卵投掷”模式。
        public float forceLovinResistanceReduction = 2f;  // 【繁殖】对囚犯使用“强制求爱”时，降低其抵抗值的量。
        public float forceLovinWillReduction = 0.1f;      // 【繁殖】对奴隶使用“强制求爱”时，降低其意志值的量。
        public float forceLovinCertaintyReduction = 0.1f; // 【繁殖】对异教徒使用“强制求爱”时，降低其信仰确定性的量。
        public float forceLovinInstantRecruitChance = 0.1f; //【繁殖】通过“强制求爱”将抵抗/意志降为0后，直接成功招募/奴役的概率。
        public float forceLovinBreakLoyaltyChance = 0.05f;// 【繁殖】对拥有“忠诚”思想的囚犯/奴隶使用“强制求爱”时，直接破除其忠诚的概率。
        public float spiritEggWarmthDays = 3f;            // 【繁殖】灵卵在体内达到“完美温养”状态所需的天数。
        public bool enableGrandClimax = false;            // 【武器-彩蛋】是否启用“灵卵拉珠”的大招“盛大高潮”。
        public bool enableDimensionalSex = false;         // 【物品-彩蛋】是否启用飞机杯的“次元性交”功能。
        public bool rjwRavenPregnancyCompat = true;       // 【兼容性】是否启用与RJW的特殊怀孕兼容逻辑。

        // ===================================================
        // 5. 杂交兼容 (Hybridization)
        // ===================================================
        public bool enableMiliraCompat = true;
        public bool enableMoeLotlCompat = true;
        public bool enableKoelimeBloodline = true;
        public bool enableMuGirlCompat = true;
        public bool enableMuffaloPrank = false;
        public bool enableWolfeinCompat = true;
        public bool enableDragonianCompat = true;
        public bool enableMoyoCompat = true;
        public bool enableEponaCompat = true;
        public bool enableTailinCompat = true;
        public bool enableCinderCompat = true;
        public bool enableMiraboreasCompat = true;
        public bool enableMinchoCompat = true;
        public bool enableNemesisCompat = true;
        public bool enableGoldenGloriaCompat = true;
        public bool enableNivarianCompat = true;

        // ===================================================
        // 6. 扶桑组织 (Fusang)
        // ===================================================
        public float fusangCommCooldownDays = 3f;
        public float reinforcementDelayHours = 6f;
        public float tradePriceModifier = 1.0f;
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
        public int risingWallSize = 5;

        // ===================================================
        // 9. 建筑与娱乐 (Buildings & Entertainment)
        // ===================================================
        public float incenseJoyAmount = 0.05f;
        public float incenseForceLovinChance = 0.05f;
        public int incenseCheckInterval = 250;
        public float avMatingChance = 0.02f;
        public float avJoyWeightMultiplier = 1.0f;
        public bool avDisableTolerance = false;
        public float bathtubJoyWeightMultiplier = 1.0f;
        public bool bathtubDisableTolerance = false;

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

            // --- 1. 基础与调试 ---
            Scribe_Values.Look(ref enableDebugMode, "enableDebugMode", false);
            Scribe_Values.Look(ref enableVerboseLogging, "enableVerboseLogging", false);
            Scribe_Values.Look(ref enableMemeSounds, "enableMemeSounds", false);
            Scribe_Values.Look(ref emberBloodDeathChance, "emberBloodDeathChance", 0.3f);
            Scribe_Values.Look(ref emberBloodBerserkChance, "emberBloodBerserkChance", 0.3f);
            Scribe_Values.Look(ref featherDropMoodThreshold, "featherDropMoodThreshold", 0.05f);
            Scribe_Values.Look(ref featherDropChance, "featherDropChance", 0.02f);
            Scribe_Values.Look(ref featherCooldownDays, "featherCooldownDays", 60f);
            Scribe_Values.Look(ref showFeatherCooldown, "showFeatherCooldown", false);
            Scribe_Values.Look(ref enableGreatRavenShiny, "enableGreatRavenShiny", true);
            Scribe_Values.Look(ref greatRavenSearchDays, "greatRavenSearchDays", 3.0f);
            Scribe_Values.Look(ref greatRavenGoldChance, "greatRavenGoldChance", 0.95f);
            Scribe_Values.Look(ref greatRavenItemChance, "greatRavenItemChance", 0.04f);
            Scribe_Values.Look(ref greatRavenCubeChance, "greatRavenCubeChance", 0.01f);

            Scribe_Values.Look(ref servitudeInteractionChance, "servitudeInteractionChance", 0.1f);            // [新增] 侍奉系统
            Scribe_Values.Look(ref servitudeCooldownMultiplier, "servitudeCooldownMultiplier", 1.0f);            // [新增] 侍奉系统

            // --- 2. 血脉系统 ---
            Scribe_Values.Look(ref bloodlineInheritanceStrength, "bloodlineInheritanceStrength", 1.0f);
            Scribe_Values.Look(ref goldenCrowDecayRate, "goldenCrowDecayRate", 0.1f);
            Scribe_Values.Look(ref enableBloodlineMutations, "enableBloodlineMutations", true);
            Scribe_Values.Look(ref purificationSuccessChance, "purificationSuccessChance", 0.5f);
            Scribe_Values.Look(ref purificationRitualDurationTicks, "purificationRitualDurationTicks", 5000);

            // --- 3. 育生祭坛 ---
            Scribe_Values.Look(ref baseHatchingDays, "baseHatchingDays", 15f);
            Scribe_Values.Look(ref altarEffectRadius, "altarEffectRadius", 5);
            Scribe_Values.Look(ref enableAltarVisualEffects, "enableAltarVisualEffects", true);

            // --- 4. 繁殖与互动 ---
            Scribe_Values.Look(ref enableForceLovinPregnancy, "enableForceLovinPregnancy", true);
            Scribe_Values.Look(ref forcedLovinPregnancyRate, "forcedLovinPregnancyRate", 0.05f);
            Scribe_Values.Look(ref forceLovinCooldownDays, "forceLovinCooldownDays", 1.0f);
            Scribe_Values.Look(ref ravenFatherDeterminesEgg, "ravenFatherDeterminesEgg", false);
            Scribe_Values.Look(ref enableMalePregnancyEgg, "enableMalePregnancyEgg", false);
            Scribe_Values.Look(ref ignoreFertilityForPregnancy, "ignoreFertilityForPregnancy", false);
            Scribe_Values.Look(ref enableSameSexForceLovin, "enableSameSexForceLovin", true);
            Scribe_Values.Look(ref enableMechanoidLovin, "enableMechanoidLovin", false);
            Scribe_Values.Look(ref enableBuildingLovin, "enableBuildingLovin", false);
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
            Scribe_Values.Look(ref rjwRavenPregnancyCompat, "rjwRavenPregnancyCompat", true);

            // --- 5. 杂交兼容 ---
            Scribe_Values.Look(ref enableMiliraCompat, "enableMiliraCompat", true);
            Scribe_Values.Look(ref enableMoeLotlCompat, "enableMoeLotlCompat", true);
            Scribe_Values.Look(ref enableKoelimeBloodline, "enableKoelimeBloodline", true);
            Scribe_Values.Look(ref enableMuGirlCompat, "enableMuGirlCompat", true);
            Scribe_Values.Look(ref enableMuffaloPrank, "enableMuffaloPrank", false);
            Scribe_Values.Look(ref enableWolfeinCompat, "enableWolfeinCompat", true);
            Scribe_Values.Look(ref enableDragonianCompat, "enableDragonianCompat", true);
            Scribe_Values.Look(ref enableMoyoCompat, "enableMoyoCompat", true);
            Scribe_Values.Look(ref enableEponaCompat, "enableEponaCompat", true);
            Scribe_Values.Look(ref enableTailinCompat, "enableTailinCompat", true);
            Scribe_Values.Look(ref enableCinderCompat, "enableCinderCompat", true);
            Scribe_Values.Look(ref enableMiraboreasCompat, "enableMiraboreasCompat", true);
            Scribe_Values.Look(ref enableMinchoCompat, "enableMinchoCompat", true);
            Scribe_Values.Look(ref enableNemesisCompat, "enableNemesisCompat", true);
            Scribe_Values.Look(ref enableGoldenGloriaCompat, "enableGoldenGloriaCompat", true);
            Scribe_Values.Look(ref enableNivarianCompat, "enableNivarianCompat", true);

            // --- 6. 扶桑组织 ---
            Scribe_Values.Look(ref fusangCommCooldownDays, "fusangCommCooldownDays", 3f);
            Scribe_Values.Look(ref reinforcementDelayHours, "reinforcementDelayHours", 6f);
            Scribe_Values.Look(ref tradePriceModifier, "tradePriceModifier", 1.0f);
            Scribe_Values.Look(ref tradeCaravanCooldownDays, "tradeCaravanCooldownDays", 3.0f);

            // --- 7. 间谍系统 ---
            Scribe_Values.Look(ref missionSuccessBonus, "missionSuccessBonus", 0f);
            Scribe_Values.Look(ref enableManualInfiltration, "enableManualInfiltration", true);
            Scribe_Values.Look(ref missionFailureCooldownDays, "missionFailureCooldownDays", 7f);
            Scribe_Values.Look(ref missionCostMultiplier, "missionCostMultiplier", 1.0f);
            Scribe_Values.Look(ref missionDurationMultiplier, "missionDurationMultiplier", 1.0f);

            // --- 8. 防卫系统 ---
            Scribe_Values.Look(ref enableDefenseSystemDebug, "enableDefenseSystemDebug", false);
            Scribe_Values.Look(ref trapDamageMultiplier, "trapDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref friendlyFireSafe, "friendlyFireSafe", true);
            Scribe_Values.Look(ref risingWallSize, "risingWallSize", 5);

            // --- 9. 建筑与娱乐 ---
            Scribe_Values.Look(ref incenseJoyAmount, "incenseJoyAmount", 0.05f);
            Scribe_Values.Look(ref incenseForceLovinChance, "incenseForceLovinChance", 0.05f);
            Scribe_Values.Look(ref incenseCheckInterval, "incenseCheckInterval", 250);
            Scribe_Values.Look(ref avMatingChance, "avMatingChance", 0.02f);
            Scribe_Values.Look(ref avJoyWeightMultiplier, "avJoyWeightMultiplier", 1.0f);
            Scribe_Values.Look(ref avDisableTolerance, "avDisableTolerance", false);
            Scribe_Values.Look(ref bathtubJoyWeightMultiplier, "bathtubJoyWeightMultiplier", 1.0f);
            Scribe_Values.Look(ref bathtubDisableTolerance, "bathtubDisableTolerance", false);
        }

        public void OnSettingsChanged()
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                RavenWallColorPatch.ClearCache();

                foreach (Pawn p in PawnsFinder.AllMaps_FreeColonistsSpawned)
                {
                    p.Drawer?.renderer?.SetAllGraphicsDirty();
                }
            }
        }
    }
}