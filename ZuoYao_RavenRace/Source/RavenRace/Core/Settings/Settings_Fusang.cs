using UnityEngine;
using Verse;

namespace RavenRace.Settings
{
    public static class Settings_Fusang
    {
        public static void Draw(Listing_Standard listing)
        {
            listing.Label("RavenRace_Settings_FusangDesc".Translate());
            listing.GapLine();
            // [Fixed] 移除所有未实装功能的参数调整
            listing.Label("RavenRace_Settings_ComingSoon".Translate());
        }
    }
}