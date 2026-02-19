using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Miraboreas
{
    [StaticConstructorOnStartup]
    public static class MiraboreasCompatUtility
    {
        public static bool IsMiraboreasActive { get; private set; }
        public static ThingDef MiraboreasRaceDef { get; private set; }
        public static HediffDef MiraboreasBloodlineHediff { get; private set; }

        static MiraboreasCompatUtility()
        {
            // 根据米拉波雷亚斯的种族DefName判断Mod是否存在
            MiraboreasRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("LBD_Fatalis_Race");
            IsMiraboreasActive = (MiraboreasRaceDef != null);

            MiraboreasBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MiraboreasBloodline");

            if (IsMiraboreasActive)
            {
                // 在开发者日志中打印兼容性激活信息
                RavenModUtility.LogVerbose("[RavenRace] Legendary Black Dragon (Miraboreas) detected. Compatibility active.");
            }
        }

        /// <summary>
        /// 检查Pawn是否拥有米拉波雷亚斯血脉。
        /// </summary>
        public static bool HasMiraboreasBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            // 使用正确的raceDef名称作为Key来检查
            return comp.BloodlineComposition.ContainsKey("LBD_Fatalis_Race") &&
                   comp.BloodlineComposition["LBD_Fatalis_Race"] > 0f;
        }

        /// <summary>
        /// 根据血脉状态，为Pawn添加或移除“黑龙血脉”Hediff。
        /// </summary>
        public static void HandleMiraboreasBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || MiraboreasBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(MiraboreasBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(MiraboreasBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MiraboreasBloodlineHediff);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }
}