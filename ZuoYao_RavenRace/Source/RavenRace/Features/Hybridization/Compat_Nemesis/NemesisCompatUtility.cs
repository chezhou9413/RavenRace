using System;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Nemesis
{
    [StaticConstructorOnStartup]
    public static class NemesisCompatUtility
    {
        public static bool IsNemesisActive { get; private set; }
        public static HediffDef NemesisBloodlineHediff { get; private set; }
        public static FleckDef NemesisTeleportFleck { get; private set; }

        public const string NemesisKey = "Nemesis_Race";

        static NemesisCompatUtility()
        {
            // 1. 检测种族是否存在
            IsNemesisActive = DefDatabase<ThingDef>.GetNamedSilentFail(NemesisKey) != null;

            if (IsNemesisActive)
            {
                // 2. 尝试获取 Hediff
                NemesisBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_NemesisBloodline");

                // [调试关键] 如果检测到了种族，但没找到 Hediff，说明 XML 的 MayRequire 写错了或者文件没加载
                if (NemesisBloodlineHediff == null)
                {
                    Log.Error($"[RavenRace] 警告：检测到纳美西斯种族 ({NemesisKey})，但无法找到 Hediff 'Raven_Hediff_NemesisBloodline'。请检查 Hediff_NemesisBloodline.xml 的 MayRequire 是否匹配 PackageId: Aurora.Nebula.NemesisRaceThePunisher");
                }
                else
                {
                    RavenModUtility.LogVerbose("[RavenRace] 纳美西斯兼容性已完全激活 (种族+Hediff均就绪)。");
                }

                // 3. 尝试获取特效 (非必须，没有就用原版)
                NemesisTeleportFleck = DefDatabase<FleckDef>.GetNamedSilentFail("Nemesis_Fleck_Anim1");
            }
        }

        public static bool HasNemesisBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;

            // 只要字典中包含 Nemesis_Race 且比例大于 0.1% 即可
            return comp.BloodlineComposition.ContainsKey(NemesisKey) &&
                   comp.BloodlineComposition[NemesisKey] > 0.001f;
        }

        public static void HandleNemesisBloodline(Pawn pawn, bool hasBloodline)
        {
            // 如果 HediffDef 为空，直接返回，避免红字
            if (pawn == null || NemesisBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(NemesisBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(NemesisBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(NemesisBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }
    }
}