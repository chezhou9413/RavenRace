using System;
using RavenRace.Compat.Cinder;
using RavenRace.Compat.Dragonian;
using RavenRace.Compat.Epona;
using RavenRace.Compat.GoldenGloria;
using RavenRace.Compat.Koelime;
using RavenRace.Compat.Milira;
using RavenRace.Compat.Mincho;
using RavenRace.Compat.Miraboreas;
using RavenRace.Compat.MoeLotl;
using RavenRace.Compat.Moyo;
using RavenRace.Compat.MuGirl;
using RavenRace.Compat.Nemesis;
using RavenRace.Compat.Nivarian;
using RavenRace.Compat.Tailin;
using RavenRace.Compat.Wolfein;
using Verse;

namespace RavenRace.Features.Bloodline
{
    /// <summary>
    /// 血脉组件 - 模组兼容性逻辑 (重构极简版)
    /// </summary>
    public partial class CompBloodline
    {
        public void RefreshAbilities()
        {
            EnsureBloodlineFloor();
            CheckAndGrantBloodlineAbilities();
        }

        private void CheckAndGrantBloodlineAbilities()
        {
            if (this.Pawn == null || this.Pawn.health == null) return;

            try
            {
                var settings = RavenRaceMod.Settings;

                // ==========================================
                // 内部特殊血脉
                // ==========================================
                bool hasMechanoid = BloodlineUtility.HasBloodline(this, BloodlineManager.MECHANIOD_BLOODLINE_KEY);
                BloodlineUtility.ToggleHediff(this.Pawn, DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MechanoidBloodline"), hasMechanoid);

                // ==========================================
                // 第三方兼容血脉
                // ==========================================

                // 1. 米莉拉
                bool hasMilira = settings.enableMiliraCompat && MiliraCompatUtility.IsMiliraActive && BloodlineUtility.HasBloodline(this, "Milira_Race", "Milira");
                BloodlineUtility.ToggleHediff(this.Pawn, MiliraCompatUtility.MiliraBloodlineHediff, hasMilira);

                // 2. 萌螈 (包含动态初始化)
                bool hasMoeLotl = settings.enableMoeLotlCompat && MoeLotlCompatUtility.IsMoeLotlActive && BloodlineUtility.HasBloodline(this, "Axolotl");
                BloodlineUtility.ToggleHediff(this.Pawn, MoeLotlCompatUtility.MoeLotlBloodlineHediff, hasMoeLotl);
                if (hasMoeLotl) MoeLotlCompatUtility.GrantCultivationAbility(this.Pawn);

                // 3. 珂莉姆
                bool hasKoelime = settings.enableKoelimeBloodline && KoelimeCompatUtility.IsKoelimeActive && BloodlineUtility.HasBloodline(this, "Alien_Koelime");
                BloodlineUtility.ToggleHediff(this.Pawn, KoelimeCompatUtility.KoelimeBloodlineHediff, hasKoelime);

                // 4. 沃芬
                bool hasWolfein = settings.enableWolfeinCompat && WolfeinCompatUtility.IsWolfeinActive && BloodlineUtility.HasBloodline(this, "Wolfein_Race");
                BloodlineUtility.ToggleHediff(this.Pawn, WolfeinCompatUtility.WolfeinBloodlineHediff, hasWolfein);

                // 5. 龙人
                bool hasDragonian = settings.enableDragonianCompat && DragonianCompatUtility.IsDragonianActive && BloodlineUtility.HasBloodline(this, "Dragonian_Race");
                BloodlineUtility.ToggleHediff(this.Pawn, DragonianCompatUtility.DragonianBloodlineHediff, hasDragonian);

                // 6. 雪牛娘 (包含技能赋予)
                bool hasMuGirl = settings.enableMuGirlCompat && MuGirlCompatUtility.IsMuGirlActive && BloodlineUtility.HasBloodline(this, "MooGirl");
                BloodlineUtility.ToggleHediff(this.Pawn, MuGirlCompatUtility.MuGirlBloodlineHediff, hasMuGirl);
                BloodlineUtility.ToggleAbility(this.Pawn, MuGirlCompatUtility.RavenChargeAbility, hasMuGirl);

                // 7. 莫约
                bool hasMoyo = settings.enableMoyoCompat && MoyoCompatUtility.IsMoyoActive && BloodlineUtility.HasBloodline(this, "Alien_Moyo");
                BloodlineUtility.ToggleHediff(this.Pawn, MoyoCompatUtility.MoyoBloodlineHediff, hasMoyo);

                // 8. 艾波娜
                bool hasEpona = settings.enableEponaCompat && EponaCompatUtility.IsEponaActive && BloodlineUtility.HasBloodline(this, "Alien_Epona", "Alien_Destrier", "Alien_Unicorn");
                BloodlineUtility.ToggleHediff(this.Pawn, EponaCompatUtility.EponaBloodlineHediff, hasEpona);

                // 9. 泰临
                bool hasTailin = settings.enableTailinCompat && TailinCompatUtility.IsTailinActive && BloodlineUtility.HasBloodline(this, "TailinRace");
                BloodlineUtility.ToggleHediff(this.Pawn, TailinCompatUtility.TailinBloodlineHediff, hasTailin);

                // 10. 烟烬
                bool hasCinder = settings.enableCinderCompat && CinderCompatUtility.IsCinderActive && BloodlineUtility.HasBloodline(this, "Alien_Cinder");
                BloodlineUtility.ToggleHediff(this.Pawn, CinderCompatUtility.CinderBloodlineHediff, hasCinder);

                // 11. 米拉波雷亚斯 (黑龙)
                bool hasMiraboreas = settings.enableMiraboreasCompat && MiraboreasCompatUtility.IsMiraboreasActive && BloodlineUtility.HasBloodline(this, "LBD_Fatalis_Race");
                BloodlineUtility.ToggleHediff(this.Pawn, MiraboreasCompatUtility.MiraboreasBloodlineHediff, hasMiraboreas);

                // 12. 珉巧
                bool hasMincho = settings.enableMinchoCompat && MinchoCompatUtility.IsMinchoActive && BloodlineUtility.HasBloodline(this, "Mincho_ThingDef");
                BloodlineUtility.ToggleHediff(this.Pawn, MinchoCompatUtility.MinchoBloodlineHediff, hasMincho);

                // 13. 纳美西斯
                bool hasNemesis = settings.enableNemesisCompat && NemesisCompatUtility.IsNemesisActive && BloodlineUtility.HasBloodline(this, "Nemesis_Race");
                BloodlineUtility.ToggleHediff(this.Pawn, NemesisCompatUtility.NemesisBloodlineHediff, hasNemesis);

                // 14. 煌金族
                bool hasGoldenGloria = settings.enableGoldenGloriaCompat && GoldenGloriaCompatUtility.IsGoldenGloriaActive && BloodlineUtility.HasBloodline(this, "GoldenGlorias");
                BloodlineUtility.ToggleHediff(this.Pawn, GoldenGloriaCompatUtility.GoldenGloriaGenotypeHediff, hasGoldenGloria);

                // 15. 涅瓦莲
                bool hasNivarian = settings.enableNivarianCompat && NivarianCompatUtility.IsNivarianActive && BloodlineUtility.HasBloodline(this, "NivarianRace_Pawn");
                BloodlineUtility.ToggleHediff(this.Pawn, NivarianCompatUtility.RavenNivarianBloodlineHediff, hasNivarian);

            }
            catch (Exception ex)
            {
                Log.Warning($"[RavenRace] 血脉状态分发遇到异常: {ex.Message}\n请检查是否有模组冲突或缺失的 XML 定义。");
            }
        }
    }
}