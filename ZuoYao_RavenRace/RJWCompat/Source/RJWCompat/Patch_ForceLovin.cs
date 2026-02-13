using HarmonyLib;
using RimWorld;
using Verse;
using RavenRace.RJWCompat.UI;
using RavenRace.Features.Reproduction; // 引用主模组的繁殖命名空间

namespace RavenRace.RJWCompat
{
    [HarmonyPatch("RavenRace.CompAbilityEffect_ForceLovin", "Apply")]
    public static class Patch_ForceLovin_Apply
    {
        [HarmonyPrefix]
        public static bool Prefix(LocalTargetInfo target, CompAbilityEffect __instance)
        {
            Log.Message($"[RavenRace RJWCompat] 'ForceLovin.Apply' Prefix patch triggered!");

            Pawn caster = __instance.parent.pawn;
            Pawn targetPawn = target.Pawn;
            if (caster == null || targetPawn == null) return false;

            // [修改] 新增逻辑：如果开启了RJW兼容模式，则在使用技能时立即触发渡鸦怀孕
            if (RavenRaceMod.Settings.rjwRavenPregnancyCompat)
            {
                // 调用一个独立的辅助方法来处理怀孕逻辑，保持代码整洁
                AttemptRavenPregnancyImmediately(caster, targetPawn);
            }

            // 无论是否触发怀孕，都弹出RJW互动选择窗口
            Find.WindowStack.Add(new Dialog_SelectRjwInteraction(caster, targetPawn));

            // 阻止原版的 ForceLovin Job 创建
            return false;
        }

        /// <summary>
        /// [新增] 立即尝试触发渡鸦灵卵怀孕的辅助方法。
        /// </summary>
        private static void AttemptRavenPregnancyImmediately(Pawn caster, Pawn partner)
        {
            Pawn carrier = null;
            Pawn donor = null;

            bool isMechanoidInvolved = caster.RaceProps.IsMechanoid || partner.RaceProps.IsMechanoid;
            bool isSameSex = caster.gender == partner.gender;

            // 确定载体和供体 (逻辑与 JobDriver_ForceLovin 一致)
            if (isMechanoidInvolved)
            {
                if (!caster.RaceProps.IsMechanoid) carrier = caster;
                else if (!partner.RaceProps.IsMechanoid) carrier = partner;
                if (carrier == null) return;
                donor = (carrier == caster) ? partner : caster;
            }
            else if (isSameSex && (RavenRaceMod.Settings.enableSameSexForceLovin || RavenRaceMod.Settings.enableMalePregnancyEgg))
            {
                donor = caster;
                carrier = partner;
            }
            else
            {
                carrier = (caster.gender == Gender.Female) ? caster : partner;
                donor = (carrier == caster) ? partner : caster;
            }

            if (carrier == null || donor == null) return;
            if (carrier.gender == Gender.Male && !RavenRaceMod.Settings.enableMalePregnancyEgg) return;

            // 只有至少一方是渡鸦族，才进行灵卵怀孕
            bool isCarrierRaven = carrier.def == RavenDefOf.Raven_Race;
            bool isDonorRaven = donor.def == RavenDefOf.Raven_Race;
            if (!isCarrierRaven && !isDonorRaven) return;

            // 检查概率
            if (!Rand.Chance(RavenRaceMod.Settings.forcedLovinPregnancyRate)) return;

            // 获取基因
            GeneSet genes;
            if (isMechanoidInvolved)
            {
                genes = new GeneSet();
                if (carrier.genes != null)
                {
                    foreach (var g in carrier.genes.GenesListForReading) genes.AddGene(g.def);
                    genes.SetNameDirect("机械混血");
                }
            }
            else
            {
                // [修正] 直接接收返回的 GeneSet 对象
                genes = PregnancyUtility.GetInheritedGeneSet(donor, carrier, out bool success);
                if (!success) return;
            }

            // 创建并添加Hediff
            var hediff = (HediffRavenPregnancy)HediffMaker.MakeHediff(RavenDefOf.Raven_Hediff_RavenPregnancy, carrier);
            hediff.Initialize(donor, genes, RavenRaceMod.Settings.forceRavenDescendant);
            carrier.health.AddHediff(hediff);
            carrier.Drawer?.renderer?.SetAllGraphicsDirty();

            Messages.Message($"{carrier.LabelShortCap} 在互动前已被强制注入了一枚渡鸦灵卵。", carrier, MessageTypeDefOf.PositiveEvent);
        }
    }
}