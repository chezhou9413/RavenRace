using UnityEngine;
using Verse;

namespace RavenRace.Settings
{
    public static class Settings_Base
    {
        public static void Draw(Listing_Standard listing)
        {
            var s = RavenRaceMod.Settings;
            listing.Label("RavenRace_Settings_BaseDesc".Translate());
            listing.GapLine();

            listing.CheckboxLabeled("RavenRace_Settings_EnableDebugMode".Translate(), ref s.enableDebugMode);
            listing.CheckboxLabeled("RavenRace_Settings_EnableVerboseLogging".Translate(), ref s.enableVerboseLogging);

            listing.GapLine();

            // --- 余烬之血设置 ---
            listing.Label("=== 余烬之血概率设置 ===");

            listing.Label($"死亡概率: {s.emberBloodDeathChance:P0}");
            s.emberBloodDeathChance = listing.Slider(s.emberBloodDeathChance, 0f, 1f);

            // [Fixed] 动态限制发狂概率，确保总和不超过 100%
            float maxBerserk = 1f - s.emberBloodDeathChance;
            if (s.emberBloodBerserkChance > maxBerserk) s.emberBloodBerserkChance = maxBerserk;

            listing.Label($"发狂概率: {s.emberBloodBerserkChance:P0} (最大可用: {maxBerserk:P0})");
            s.emberBloodBerserkChance = listing.Slider(s.emberBloodBerserkChance, 0f, maxBerserk);

            listing.Label($"剩余成功概率: {1f - s.emberBloodDeathChance - s.emberBloodBerserkChance:P0}");

            listing.Gap();

            // --- 金羽设置 ---
            listing.Label("=== 折翼金羽设置 ===");

            listing.CheckboxLabeled("显示金羽冷却状态", ref s.showFeatherCooldown, "在Pawn的检查面板中显示金羽掉落的冷却时间。");

            listing.Label($"情绪阈值 (极低<X / 极高>1-X): {s.featherDropMoodThreshold:P0}");
            s.featherDropMoodThreshold = listing.Slider(s.featherDropMoodThreshold, 0.01f, 0.2f);

            listing.Label($"每次检测掉落概率: {s.featherDropChance:P1}");
            s.featherDropChance = listing.Slider(s.featherDropChance, 0.001f, 0.1f);

            listing.Label($"掉落冷却时间: {s.featherCooldownDays:F1} 天");
            s.featherCooldownDays = listing.Slider(s.featherCooldownDays, 1f, 120f);


            // --- 大渡鸦 (大统领) 设置 ---
            listing.GapLine();
            listing.Label("=== 渡鸦大统领设置 ===");
            listing.CheckboxLabeled("启用“寻找亮闪闪”功能", ref s.enableGreatRavenShiny, "开启后，驯服的大渡鸦会周期性地叼回金质物品。");

            if (s.enableGreatRavenShiny)
            {
                listing.Label($"寻找间隔: {s.greatRavenSearchDays:F1} 天");
                s.greatRavenSearchDays = listing.Slider(s.greatRavenSearchDays, 0.1f, 10f);

                // [新增] 概率配置
                listing.Gap();
                listing.Label("寻宝概率配置 (独立判断 0-100%)");

                listing.Label($"发现黄金概率: {s.greatRavenGoldChance:P0}");
                s.greatRavenGoldChance = listing.Slider(s.greatRavenGoldChance, 0f, 1f);

                listing.Label($"发现金制品概率: {s.greatRavenItemChance:P0}");
                s.greatRavenItemChance = listing.Slider(s.greatRavenItemChance, 0f, 1f);

                listing.Label($"发现齁金魔方概率: {s.greatRavenCubeChance:P0}");
                s.greatRavenCubeChance = listing.Slider(s.greatRavenCubeChance, 0f, 1f);

                listing.SubLabel("注：如果所有概率均未触发，大统领将空手而归并提示。", 0.8f);
            }
        }



    }
}