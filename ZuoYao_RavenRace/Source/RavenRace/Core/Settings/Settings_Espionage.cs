using UnityEngine;
using Verse;

namespace RavenRace.Settings
{
    public static class Settings_Espionage
    {
        public static void Draw(Listing_Standard listing)
        {
            var s = RavenRaceMod.Settings;
            listing.Label("RavenRace_Settings_EspionageDesc".Translate());
            listing.GapLine();

            // 任务成功率加成
            listing.Label($"{"RavenRace_Settings_MissionSuccessBonus".Translate()}: {s.missionSuccessBonus:P0}");
            s.missionSuccessBonus = listing.Slider(s.missionSuccessBonus, -0.5f, 0.5f);

            // 任务失败冷却
            listing.Label($"{"RavenRace_Settings_FailureCooldown".Translate()}: {s.missionFailureCooldownDays} Days");
            s.missionFailureCooldownDays = listing.Slider(s.missionFailureCooldownDays, 1f, 30f);

            // [新增] 消耗倍率
            listing.Label($"{"RavenRace_Settings_CostMultiplier".Translate()}: {s.missionCostMultiplier:P0}");
            s.missionCostMultiplier = listing.Slider(s.missionCostMultiplier, 0.1f, 5.0f);

            // [新增] 耗时倍率
            listing.Label($"{"RavenRace_Settings_DurationMultiplier".Translate()}: {s.missionDurationMultiplier:P0}");
            s.missionDurationMultiplier = listing.Slider(s.missionDurationMultiplier, 0.1f, 5.0f);

            listing.Gap();
            listing.CheckboxLabeled("RavenRace_Settings_EnableManualInfiltration".Translate(), ref s.enableManualInfiltration);
        }
    }
}