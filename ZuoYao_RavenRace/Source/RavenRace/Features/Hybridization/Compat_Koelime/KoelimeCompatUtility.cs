using System;
using Verse;
using RimWorld;

namespace RavenRace.Compat.Koelime
{
    /// <summary>
    /// 珂莉姆种族兼容性工具类
    /// </summary>
    [StaticConstructorOnStartup]
    public static class KoelimeCompatUtility
    {
        public static bool IsKoelimeActive { get; private set; }
        public static ThingDef KoelimeRaceDef { get; private set; }
        public static HediffDef KoelimeBloodlineHediff { get; private set; }

        static KoelimeCompatUtility()
        {
            // 根据珂莉姆 XML 中的 DefName "Alien_Koelime" 查找
            KoelimeRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("Alien_Koelime");
            IsKoelimeActive = (KoelimeRaceDef != null);

            KoelimeBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_KoelimeBloodline");

            if (IsKoelimeActive)
            {
                RavenModUtility.LogVerbose("[RavenRace] Koelime (Alien_Koelime) detected. Compatibility active.");
            }
        }

        /// <summary>
        /// 赋予或移除次级古龙血脉 Hediff
        /// </summary>
        public static void HandleDraconicBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || KoelimeBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(KoelimeBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(KoelimeBloodlineHediff);
                // 仅在调试模式下输出，避免刷屏
                if (RavenRaceMod.Settings.enableVerboseLogging)
                {
                    RavenModUtility.LogVerbose($"Applied Koelime Bloodline Hediff to {pawn.LabelShort}");
                }
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(KoelimeBloodlineHediff);
                if (h != null)
                {
                    pawn.health.RemoveHediff(h);
                }
            }
        }
    }
}