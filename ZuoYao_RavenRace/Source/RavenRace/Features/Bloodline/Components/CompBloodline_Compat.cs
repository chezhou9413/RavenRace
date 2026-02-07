using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
using RavenRace.Compat.Milira;
using RavenRace.Compat.MoeLotl;
using RavenRace.Compat.Koelime;
using RavenRace.Compat.MuGirl;
using RavenRace.Compat.Wolfein;
using RavenRace.Compat.Dragonian;
using RavenRace.Compat.Moyo;
using RavenRace.Compat.Epona;
using RavenRace.Compat.Tailin;

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
            if (bloodlineComposition == null || bloodlineComposition.Count == 0) return;

            try
            {
                // 1. 米莉拉
                if (RavenRaceMod.Settings.enableMiliraFlightForHybrids &&
                    bloodlineComposition.ContainsKey("Milira_Race") &&
                    bloodlineComposition["Milira_Race"] > 0f)
                {
                    GrantMiliraFlight();
                }

                // 2. 萌螈
                if (RavenRaceMod.Settings.enableMoeLotlCompat &&
                    bloodlineComposition.ContainsKey("Axolotl") &&
                    bloodlineComposition["Axolotl"] > 0f)
                {
                    MoeLotlCompatUtility.GrantCultivationAbility(this.Pawn);
                }

                // 3. 珂莉姆
                if (RavenRaceMod.Settings.enableKoelimeBloodline)
                {
                    bool hasKoelime = bloodlineComposition.ContainsKey("Alien_Koelime") &&
                                      bloodlineComposition["Alien_Koelime"] > 0f;
                    KoelimeCompatUtility.HandleDraconicBloodline(this.Pawn, hasKoelime);
                }

                // 【核心修正】4. 沃芬 (Wolfein)
                if (RavenRaceMod.Settings.enableWolfeinCompat && WolfeinCompatUtility.IsWolfeinActive)
                {
                    // 逻辑与珂莉姆一致：先判断是否有血脉，然后让工具类处理组件的添加或移除
                    bool hasWolfein = WolfeinCompatUtility.HasWolfeinBloodline(this);
                    WolfeinCompatUtility.HandleWolfeinBloodline(this.Pawn, hasWolfein);
                }

                // 5. 产奶逻辑 (雪牛 MuGirl & 龙人 Dragonian & 混合 Combo)
                RemoveExistingMilkComp();

                bool hasMuGirl = RavenRaceMod.Settings.enableMuGirlCompat && MuGirlCompatUtility.HasMuGirlBloodline(this);
                bool hasDragonian = RavenRaceMod.Settings.enableDragonianCompat && DragonianCompatUtility.HasDragonianBloodline(this);

                if (RavenRaceMod.Settings.enableDragonianCompat)
                {
                    DragonianCompatUtility.HandleDragonianBuff(this.Pawn, hasDragonian);
                }
                if (RavenRaceMod.Settings.enableMuGirlCompat)
                {
                    MuGirlCompatUtility.HandleMuGirlBloodline(this.Pawn, hasMuGirl);
                    MuGirlCompatUtility.HandleChargeAbility(this.Pawn, hasMuGirl);
                }

                if (hasDragonian && hasMuGirl)
                {
                    DragonianCompatUtility.GrantDivineMilkAbility(this.Pawn);
                }
                else if (hasDragonian)
                {
                    DragonianCompatUtility.GrantDragonMilkAbility(this.Pawn);
                }
                else if (hasMuGirl)
                {
                    MuGirlCompatUtility.EnsureMilkable(this.Pawn);
                }

                // 6. 莫约 (Moyo)
                if (RavenRaceMod.Settings.enableMoyoCompat && MoyoCompatUtility.HasMoyoBloodline(this.Pawn))
                {
                    MoyoCompatUtility.GrantDeepBlueProduction(this.Pawn);
                }

                // 7. 艾波娜 (Epona)
                if (EponaCompatUtility.IsEponaActive && EponaCompatUtility.HasEponaBloodline(this.Pawn))
                {
                    EponaCompatUtility.EnsureEponaHybridComp(this.Pawn);
                }

                // 刷新雪牛冲锋逻辑 (受艾波娜影响)
                if (RavenRaceMod.Settings.enableMuGirlCompat && MuGirlCompatUtility.HasMuGirlBloodline(this))
                {
                    MuGirlCompatUtility.HandleChargeAbility(this.Pawn, true);
                }

                // 8. 泰临 (Tailin)
                if (RavenRaceMod.Settings.enableTailinCompat && TailinCompatUtility.IsTailinActive)
                {
                    bool hasTailinBlood = TailinCompatUtility.HasTailinBloodline(this);
                    TailinCompatUtility.HandleTailinBloodline(this.Pawn, hasTailinBlood);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RavenRace] 血脉组件访问失效: {ex.Message}");
            }
        }

        private void RemoveExistingMilkComp()
        {
            Pawn.AllComps.RemoveAll(c => c is Compat.MuGirl.CompRavenMilkable);
        }

        private void GrantMiliraFlight()
        {
            var existingComp = Pawn.TryGetComp<CompFlightControl>();
            if (existingComp != null) return;

            try
            {
                var proxyComp = new CompFlightControl();
                proxyComp.parent = this.parent;
                proxyComp.Initialize(new CompProperties_FlightControl());
                Pawn.AllComps.Add(proxyComp);
                if (Pawn.Spawned) proxyComp.PostSpawnSetup(false);
            }
            catch (Exception ex)
            {
                Log.Warning($"[RavenRace] Failed to grant Milira flight: {ex.Message}");
            }
        }
    }
}