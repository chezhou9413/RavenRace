using UnityEngine;
using Verse;

namespace RavenRace.Settings
{
    public static class Settings_Buildings
    {
        public static void Draw(Listing_Standard listing)
        {
            var s = RavenRaceMod.Settings;

            // ==========================================
            // 1. 催情香炉
            // ==========================================
            listing.Label("=== 催情香炉设置 ===");
            listing.Label($"强制求爱概率 (每次判定): {s.incenseForceLovinChance.ToStringPercent()}");
            s.incenseForceLovinChance = listing.Slider(s.incenseForceLovinChance, 0f, 1f);
            listing.GapLine();

            // ==========================================
            // 2. 电视机功能强化
            // ==========================================
            listing.Label("=== 电视机功能设置 ===");

            // A. 交配触发率
            listing.Label($"AV播放时交配触发率: {s.avMatingChance.ToStringPercent()}");
            s.avMatingChance = listing.Slider(s.avMatingChance, 0f, 1f);

            // B. 吸引力权重
            listing.Label($"电视娱乐吸引力倍率: {s.avJoyWeightMultiplier:F1}x");
            // 【1.6 适配】使用反编译代码中确认存在的 SubLabel 方法 (GameFont.Tiny + Color.gray)
            listing.SubLabel("数值越大，小人越优先看电视。设为 100x 时将几乎只进行电视娱乐。", 1f);
            s.avJoyWeightMultiplier = listing.Slider(s.avJoyWeightMultiplier, 1.0f, 100.0f);

            // C. 厌倦屏蔽
            listing.CheckboxLabeled("禁用电视娱乐厌倦感", ref s.avDisableTolerance, "开启后，电视娱乐产生的耐受度永远为0，获得的娱乐值不会随次数衰减。");

            listing.GapLine();
            listing.SubLabel("说明：开启电视AV模式后，至少两名观看者同时在场时，有概率无视条件发生交配。", 1f);
            listing.Gap();
        }
    }
}