using UnityEngine;
using Verse;
using RavenRace.Compat.MoeLotl;
using RavenRace.Compat.Koelime;
using RavenRace.Compat.MuGirl;
using RavenRace.Compat.Milira;
using RavenRace.Compat.Wolfein;
using RavenRace.Compat.Dragonian;
using RavenRace.Compat.Moyo;
using RavenRace.Compat.Epona; 
using RavenRace.Compat.Tailin;
using RavenRace.Compat.Cinder;

// 记得每加一个都要引用命名空间！

namespace RavenRace.Settings
{
    public static class Settings_Hybridization
    {
        public static void Draw(Listing_Standard listing)
        {
            var s = RavenRaceMod.Settings;

            listing.Label("RavenRace_Settings_HybridizationDesc".Translate());
            listing.Gap();

            // 统一使用 GapLine + Label 的标准形式

            // =================================================
            // 1. 米莉拉 (Milira)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_MiliraCompat".Translate());
            DrawModStatus(listing, MiliraCompatUtility.IsMiliraActive);

            // 替换为：
            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableMiliraCompat".Translate(),
                ref s.enableMiliraCompat,
                "RavenRace_Settings_EnableMiliraCompat_Desc".Translate()
            );

            // =================================================
            // 2. 萌螈 (MoeLotl)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_MoeLotlCultivation".Translate());
            DrawModStatus(listing, MoeLotlCompatUtility.IsMoeLotlActive);

            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableMoeLotlCompat".Translate(),
                ref s.enableMoeLotlCompat,
                "RavenRace_Settings_EnableMoeLotlCompat_Desc".Translate()
            );

            // =================================================
            // 3. 珂莉姆 (Koelime)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_KlimDragonBlood".Translate());
            DrawModStatus(listing, KoelimeCompatUtility.IsKoelimeActive);

            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableKoelimeBloodline".Translate(),
                ref s.enableKoelimeBloodline,
                "RavenRace_Settings_EnableKoelimeBloodline_Desc".Translate()
            );

            // =================================================
            // 4. 雪牛娘 (MooGirl)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_MuGirlCompat".Translate());
            DrawModStatus(listing, MuGirlCompatUtility.IsMuGirlActive);

            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableMuGirlCompat".Translate(),
                ref s.enableMuGirlCompat,
                "RavenRace_Settings_EnableMuGirlCompat_Desc".Translate()
            );

            listing.Gap(4f);
            listing.CheckboxLabeled(
                "RavenRace_Settings_MuffaloPrank".Translate(),
                ref s.enableMuffaloPrank,
                "RavenRace_Settings_MuffaloPrank_Desc".Translate()
            );

            // =================================================
            // 5. 沃芬 (Wolfein)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_WolfeinCompat".Translate());
            DrawModStatus(listing, WolfeinCompatUtility.IsWolfeinActive);

            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableWolfeinCompat".Translate(),
                ref s.enableWolfeinCompat,
                "RavenRace_Settings_EnableWolfeinCompat_Desc".Translate()
            );

            // =================================================
            // 6. 龙人 (Dragonian)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_DragonianCompat".Translate());
            DrawModStatus(listing, DragonianCompatUtility.IsDragonianActive);

            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableDragonianCompat".Translate(),
                ref s.enableDragonianCompat,
                "RavenRace_Settings_EnableDragonianCompat_Desc".Translate()
            );

            // =================================================
            // 7. 茉约 (Moyo)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_MoyoCompat".Translate());
            DrawModStatus(listing, MoyoCompatUtility.IsMoyoActive);

            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableMoyoCompat".Translate(),
                ref s.enableMoyoCompat,
                "RavenRace_Settings_EnableMoyoCompat_Desc".Translate()
            );


            // =================================================
            // 8. 艾波娜 (Epona)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_EponaCompat".Translate());
            DrawModStatus(listing, EponaCompatUtility.IsEponaActive);

            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableEponaCompat".Translate(),
                ref s.enableEponaCompat,
                "RavenRace_Settings_EnableEponaCompat_Desc".Translate()
            );

            // =================================================
            // 9. 泰临 (Tailin)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_TailinCompat".Translate());
            DrawModStatus(listing, TailinCompatUtility.IsTailinActive);

            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableTailinCompat".Translate(),
                ref s.enableTailinCompat,
                "RavenRace_Settings_EnableTailinCompat_Desc".Translate()
            );

            // =================================================
            // 10. 烟烬 (Cinder)
            // =================================================
            listing.GapLine();
            listing.Label("RavenRace_Settings_CinderCompat".Translate());
            DrawModStatus(listing, CinderCompatUtility.IsCinderActive);

            listing.CheckboxLabeled(
                "RavenRace_Settings_EnableCinderCompat".Translate(),
                ref s.enableCinderCompat,
                "RavenRace_Settings_EnableCinderCompat_Desc".Translate()
            );





        }

        private static void DrawModStatus(Listing_Standard listing, bool isActive)
        {
            Color originalColor = GUI.color;
            if (isActive)
            {
                GUI.color = Color.green;
                listing.Label("RavenRace_Settings_ModActive".Translate());
            }
            else
            {
                GUI.color = Color.red;
                listing.Label("RavenRace_Settings_ModInactive".Translate());
            }
            GUI.color = originalColor;
        }
    }
}