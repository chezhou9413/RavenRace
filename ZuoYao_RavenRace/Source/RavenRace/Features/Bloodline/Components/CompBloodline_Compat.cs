using System;
using System.Collections.Generic;
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
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RavenRace.Features.Bloodline
{
    /// <summary>
    /// 血脉组件 - 模组兼容性逻辑
    /// </summary>
    public partial class CompBloodline
    {
        public void RefreshAbilities()
        {
            EnsureBloodlineFloor(); // 每次刷新都检查保底
            CheckAndGrantBloodlineAbilities();
        }

        private void CheckAndGrantBloodlineAbilities()
        {
            // 【核心优化 1】：安全前置检查，防止由于人物在某些特殊状态下无 health 组件导致的报错
            if (this.Pawn == null || this.Pawn.health == null) return;

            // 【核心优化 2】：移除了如果字典为空就 return 的逻辑。
            // 因为当字典被完全清空时，必须继续往下执行，才能触发各个 Handle 方法去移除身上残余的 Hediff！

            try
            {
                // [新增] 机械体血脉处理
                bool hasMechanoid = bloodlineComposition != null &&
                                    bloodlineComposition.ContainsKey(BloodlineManager.MECHANIOD_BLOODLINE_KEY) &&
                                    bloodlineComposition[BloodlineManager.MECHANIOD_BLOODLINE_KEY] > 0f;
                HandleMechanoidBloodline(this.Pawn, hasMechanoid);

                // 1. 米莉拉
                if (RavenRaceMod.Settings.enableMiliraCompat && MiliraCompatUtility.IsMiliraActive)
                {
                    bool hasMilira = MiliraCompatUtility.HasMiliraBloodline(this);
                    MiliraCompatUtility.HandleMiliraBuff(this.Pawn, hasMilira);
                }

                // 2. 萌螈 (仅增加展示Hediff的调用)
                if (RavenRaceMod.Settings.enableMoeLotlCompat && MoeLotlCompatUtility.IsMoeLotlActive)
                {
                    bool hasMoeLotl = bloodlineComposition != null &&
                                      bloodlineComposition.ContainsKey("Axolotl") &&
                                      bloodlineComposition["Axolotl"] > 0f;

                    if (hasMoeLotl)
                    {
                        MoeLotlCompatUtility.GrantCultivationAbility(this.Pawn);
                        MoeLotlCompatUtility.HandleMoeLotlBloodline(this.Pawn, true);
                    }
                    else
                    {
                        MoeLotlCompatUtility.HandleMoeLotlBloodline(this.Pawn, false);
                    }
                }

                // 3. 珂莉姆
                if (RavenRaceMod.Settings.enableKoelimeBloodline && KoelimeCompatUtility.IsKoelimeActive)
                {
                    bool hasKoelime = bloodlineComposition != null &&
                                      bloodlineComposition.ContainsKey("Alien_Koelime") &&
                                      bloodlineComposition["Alien_Koelime"] > 0f;
                    KoelimeCompatUtility.HandleDraconicBloodline(this.Pawn, hasKoelime);
                }

                // 4. 沃芬 (Wolfein)
                if (RavenRaceMod.Settings.enableWolfeinCompat && WolfeinCompatUtility.IsWolfeinActive)
                {
                    bool hasWolfein = WolfeinCompatUtility.HasWolfeinBloodline(this.Pawn);
                    WolfeinCompatUtility.HandleWolfeinBloodline(this.Pawn, hasWolfein);
                }

                // 5. 产奶逻辑 (雪牛 MuGirl & 龙人 Dragonian)
                if (RavenRaceMod.Settings.enableDragonianCompat && DragonianCompatUtility.IsDragonianActive)
                {
                    bool hasDragonian = DragonianCompatUtility.HasDragonianBloodline(this);
                    DragonianCompatUtility.HandleDragonianBuff(this.Pawn, hasDragonian);
                }

                if (RavenRaceMod.Settings.enableMuGirlCompat && MuGirlCompatUtility.IsMuGirlActive)
                {
                    bool hasMuGirl = MuGirlCompatUtility.HasMuGirlBloodline(this);
                    MuGirlCompatUtility.HandleMuGirlBloodline(this.Pawn, hasMuGirl);
                    MuGirlCompatUtility.HandleChargeAbility(this.Pawn, hasMuGirl);
                }

                // 6. 莫约 (Moyo)
                if (RavenRaceMod.Settings.enableMoyoCompat && MoyoCompatUtility.IsMoyoActive)
                {
                    bool hasMoyo = MoyoCompatUtility.HasMoyoBloodline(this.Pawn);
                    MoyoCompatUtility.HandleMoyoBloodline(this.Pawn, hasMoyo);
                }

                // 7. 艾波娜 (Epona)
                if (RavenRaceMod.Settings.enableEponaCompat && EponaCompatUtility.IsEponaActive)
                {
                    bool hasEpona = EponaCompatUtility.HasEponaBloodline(this.Pawn);
                    EponaCompatUtility.HandleEponaBloodline(this.Pawn, hasEpona);
                }

                // 刷新雪牛冲锋逻辑 (受艾波娜影响，需在两者检测后再次确认)
                if (RavenRaceMod.Settings.enableMuGirlCompat && MuGirlCompatUtility.IsMuGirlActive)
                {
                    bool hasMuGirl = MuGirlCompatUtility.HasMuGirlBloodline(this);
                    MuGirlCompatUtility.HandleChargeAbility(this.Pawn, hasMuGirl);
                }

                // 8. 泰临 (Tailin)
                if (RavenRaceMod.Settings.enableTailinCompat && TailinCompatUtility.IsTailinActive)
                {
                    bool hasTailinBlood = TailinCompatUtility.HasTailinBloodline(this);
                    TailinCompatUtility.HandleTailinBloodline(this.Pawn, hasTailinBlood);
                }

                // 9. 烟烬 (Cinder)
                if (RavenRaceMod.Settings.enableCinderCompat && CinderCompatUtility.IsCinderActive)
                {
                    bool hasCinder = CinderCompatUtility.HasCinderBloodline(this);
                    CinderCompatUtility.HandleCinderRegen(this.Pawn, hasCinder);
                }

                // 10. 米拉波雷亚斯 (Miraboreas)
                if (RavenRaceMod.Settings.enableMiraboreasCompat && MiraboreasCompatUtility.IsMiraboreasActive)
                {
                    bool hasBloodline = MiraboreasCompatUtility.HasMiraboreasBloodline(this);
                    MiraboreasCompatUtility.HandleMiraboreasBloodline(this.Pawn, hasBloodline);
                }

                // 11. 珉巧( Mincho) 
                if (RavenRaceMod.Settings.enableMinchoCompat && MinchoCompatUtility.IsMinchoActive)
                {
                    bool hasMincho = MinchoCompatUtility.HasMinchoBloodline(this);
                    MinchoCompatUtility.HandleMinchoBloodline(this.Pawn, hasMincho);
                }

                // 12. 纳美西斯 (Nemesis)
                if (RavenRaceMod.Settings.enableNemesisCompat && NemesisCompatUtility.IsNemesisActive)
                {
                    bool hasNemesis = NemesisCompatUtility.HasNemesisBloodline(this);
                    NemesisCompatUtility.HandleNemesisBloodline(this.Pawn, hasNemesis);
                }

                // 13. 煌金族 (Golden Gloria) 
                if (RavenRaceMod.Settings.enableGoldenGloriaCompat && GoldenGloriaCompatUtility.IsGoldenGloriaActive)
                {
                    bool hasGoldenGloria = GoldenGloriaCompatUtility.HasGoldenGloriaBloodline(this);
                    GoldenGloriaCompatUtility.HandleGoldenGloriaBloodline(this.Pawn, hasGoldenGloria);
                }

                // 14. 涅瓦莲 (Nivarian)
                if (RavenRaceMod.Settings.enableNivarianCompat && NivarianCompatUtility.IsNivarianActive)
                {
                    bool hasNivarian = NivarianCompatUtility.HasNivarianBloodline(this);
                    NivarianCompatUtility.HandleNivarianBloodline(this.Pawn, hasNivarian);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RavenRace] 血脉组件特性分发遇到异常: {ex.Message}\n这可能导致部分血脉状态未正确刷新。");
            }
        }

        private void HandleMechanoidBloodline(Pawn pawn, bool hasBloodline)
        {
            HediffDef mechHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MechanoidBloodline");
            if (mechHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(mechHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(mechHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(mechHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }
    }
}