using UnityEngine;
using Verse;
using System;

namespace RavenRace.Settings
{
    public static class Settings_Defense
    {
        public static void Draw(Listing_Standard listing)
        {
            listing.Label("RavenRace_Settings_DefenseSystem".Translate());
            listing.GapLine();

            var s = RavenRaceMod.Settings;

            // 友军安全
            listing.CheckboxLabeled("RavenRace_Settings_FriendlyFireSafe".Translate(), ref s.friendlyFireSafe, "RavenRace_Settings_FriendlyFireSafeDesc".Translate());

            // 伤害倍率
            listing.Label("RavenRace_Settings_TrapDamageMultiplier".Translate() + ": " + s.trapDamageMultiplier.ToStringPercent());
            s.trapDamageMultiplier = listing.Slider(s.trapDamageMultiplier, 0.1f, 5.0f);

            listing.Gap();

            // [修复] 严格的类型转换，防止将 float 写入 xml 导致下次读取报错
            listing.Label("RavenRace_Settings_RisingWallSize".Translate() + ": " + s.risingWallSize + "x" + s.risingWallSize);

            float currentSize = (float)s.risingWallSize;
            float newSize = listing.Slider(currentSize, 5f, 13f);

            // 转换为奇数 int
            int intSize = Mathf.RoundToInt(newSize);
            if (intSize % 2 == 0) intSize++; // 偶数变奇数

            s.risingWallSize = intSize;

            listing.Gap();
            listing.CheckboxLabeled("RavenRace_Settings_DefenseDebug".Translate(), ref s.enableDefenseSystemDebug, "RavenRace_Settings_DefenseDebugDesc".Translate());
        }
    }
}