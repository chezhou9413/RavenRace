using UnityEngine;
using Verse;

namespace RavenRace.Settings
{
    public static class Settings_Altar
    {
        public static void Draw(Listing_Standard listing)
        {
            var s = RavenRaceMod.Settings;
            listing.Label("RavenRace_Settings_AltarDesc".Translate());
            listing.GapLine();

            // 仅保留基础孵化时间 (这个是核心功能需要的)
            listing.Label("RavenRace_Settings_BaseHatchingDays".Translate() + ": " + s.baseHatchingDays.ToString("0.0") + " " + "Days".Translate());
            s.baseHatchingDays = listing.Slider(s.baseHatchingDays, 1f, 60f);
        }
    }
}