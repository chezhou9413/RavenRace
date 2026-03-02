using UnityEngine;
using Verse;

namespace RavenRace.Settings
{
    /// <summary>
    /// 负责在Mod设置窗口中绘制“血脉系统”标签页下的UI内容。
    /// </summary>
    public static class Settings_Bloodline
    {
        public static void Draw(Listing_Standard listing)
        {
            // 获取全局设置实例的引用，方便调用
            var s = RavenRaceMod.Settings;

            listing.Label("RavenRace_Settings_BloodlineDesc".Translate());
            listing.GapLine();

            // 1. 血脉遗传强度
            listing.Label($"{"RavenRace_Settings_BloodlineInheritanceStrength".Translate()}: {s.bloodlineInheritanceStrength:P0}");
            s.bloodlineInheritanceStrength = listing.Slider(s.bloodlineInheritanceStrength, 0f, 1f);
            listing.Gap();

            // 2. 金乌血脉衰减速率
            listing.Label($"{"RavenRace_Settings_GoldenCrowDecayRate".Translate()}: {s.goldenCrowDecayRate:P0} / 天");
            s.goldenCrowDecayRate = listing.Slider(s.goldenCrowDecayRate, 0f, 0.5f);
            listing.Gap();

            // 3. 启用血脉突变
            listing.CheckboxLabeled("RavenRace_Settings_EnableBloodlineMutations".Translate(), ref s.enableBloodlineMutations, "RavenRace_Settings_EnableBloodlineMutationsDesc".Translate());


            // =========================================================
            // [新增] 纯化系统相关设置
            // =========================================================
            listing.GapLine();
            listing.Label("=== 纯化仪式设置 ===");

            listing.Label($"纯化仪式成功率: {s.purificationSuccessChance:P0}");
            s.purificationSuccessChance = listing.Slider(s.purificationSuccessChance, 0f, 1f);

            listing.Label($"纯化仪式读条时间 (游戏刻, 1秒=60): {s.purificationRitualDurationTicks}");
            // 滑动范围：从 1000(约16秒) 到 10000(约166秒)
            s.purificationRitualDurationTicks = (int)listing.Slider(s.purificationRitualDurationTicks, 1000f, 10000f);




        }
    }
}