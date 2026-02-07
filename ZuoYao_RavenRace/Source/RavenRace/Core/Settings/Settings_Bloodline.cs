using UnityEngine;
using Verse;

namespace RavenRace.Settings
{
    public static class Settings_Bloodline
    {
        public static void Draw(Listing_Standard listing)
        {
            // [Fixed] 移除了所有未授权的滑块
            listing.Label("RavenRace_Settings_BloodlineDesc".Translate());
            listing.GapLine();

            // 目前没有可调节项，显示一条提示即可
            listing.Label("RavenRace_Settings_ComingSoon".Translate());
        }
    }
}