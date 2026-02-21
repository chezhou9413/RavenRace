using System;
using System.Collections.Generic;
using RavenRace.Compat.Cinder;
using RavenRace.Compat.Dragonian;
using RavenRace.Compat.Epona;
using RavenRace.Compat.Koelime;
using RavenRace.Compat.Milira;
using RavenRace.Compat.Mincho; // 【新增】
using RavenRace.Compat.Miraboreas;
using RavenRace.Compat.MoeLotl;
using RavenRace.Compat.Moyo;
using RavenRace.Compat.MuGirl;
using RavenRace.Compat.Nemesis;
using RavenRace.Compat.Tailin;
using RavenRace.Compat.Wolfein;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

// 记得每加一个都要引用命名空间！

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

                // [新增] 机械体血脉处理
                if (bloodlineComposition.ContainsKey(BloodlineManager.MECHANIOD_BLOODLINE_KEY) &&
                    bloodlineComposition[BloodlineManager.MECHANIOD_BLOODLINE_KEY] > 0f)
                {
                    HandleMechanoidBloodline(this.Pawn, true);
                }
                else
                {
                    HandleMechanoidBloodline(this.Pawn, false);
                }



                // 1. 米莉拉
                if (RavenRaceMod.Settings.enableMiliraCompat && MiliraCompatUtility.IsMiliraActive)
                {
                    bool hasMilira = MiliraCompatUtility.HasMiliraBloodline(this);
                    MiliraCompatUtility.HandleMiliraBuff(this.Pawn, hasMilira);
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

                // 4. 沃芬 (Wolfein)
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

                // 12. 纳美西斯 (Nemesis) - 核心修复：确保这里被正确调用
                if (RavenRaceMod.Settings.enableNemesisCompat && NemesisCompatUtility.IsNemesisActive)
                {
                    bool hasNemesis = NemesisCompatUtility.HasNemesisBloodline(this);
                    // 强制执行状态同步
                    NemesisCompatUtility.HandleNemesisBloodline(this.Pawn, hasNemesis);
                }











            }
            catch (Exception ex)
            {
                Log.Warning($"[RavenRace] 血脉组件访问失效: {ex.Message}");
            }
        }


        private void HandleMechanoidBloodline(Pawn pawn, bool hasBloodline) //机械体处理
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


        private void RemoveExistingMilkComp()
        {
            Pawn.AllComps.RemoveAll(c => c is Compat.MuGirl.CompRavenMilkable);
        }


    }
}