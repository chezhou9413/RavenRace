using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Miraboreas
{
    [StaticConstructorOnStartup]
    public static class MiraboreasCompatUtility
    {
        public static bool IsMiraboreasActive { get; private set; }
        public static HediffDef MiraboreasBloodlineHediff { get; private set; }

        static MiraboreasCompatUtility()
        {
            // 【核心规范】：直接通过 PackageId 精准判定模组是否激活
            IsMiraboreasActive = ModsConfig.IsActive("Tourswen.LegendaryBlackDragon");

            if (IsMiraboreasActive)
            {
                MiraboreasBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MiraboreasBloodline");

                // =========================================================================
                // 【核心防御】：剥离黑龙的毒性组件，防止存档崩溃
                // 黑龙的 LegendaryBlackDragon.CompHediffGiver 存在保存 BodyPartRecord 时的底层崩溃 Bug。
                // 渡鸦继承自 BasePawn，会无辜继承这个组件。我们在这里强行从渡鸦身上将其剔除！
                // =========================================================================
                ThingDef ravenDef = DefDatabase<ThingDef>.GetNamedSilentFail("Raven_Race");
                if (ravenDef != null && ravenDef.comps != null)
                {
                    int removedCount = ravenDef.comps.RemoveAll(c =>
                        c.GetType().Name.Contains("CompProperties_HediffGiver") &&
                        c.GetType().Namespace == "LegendaryBlackDragon");

                    if (removedCount > 0)
                    {
                        Log.Message($"[RavenRace] 成功从渡鸦种族剥离了 {removedCount} 个传奇黑龙的致命 Bug 组件 (CompHediffGiver)。");
                    }
                }

                RavenModUtility.LogVerbose("[RavenRace] Legendary Black Dragon (Miraboreas) compatibility active.");
            }
        }

        public static bool HasMiraboreasBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            // 对应 Bloodline_Miraboreas.xml 中的 <raceDef>LBD_Fatalis_Race</raceDef>
            return comp.BloodlineComposition.ContainsKey("LBD_Fatalis_Race") &&
                   comp.BloodlineComposition["LBD_Fatalis_Race"] > 0f;
        }

        public static void HandleMiraboreasBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || MiraboreasBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(MiraboreasBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(MiraboreasBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MiraboreasBloodlineHediff);
                if (hediff != null) pawn.health.RemoveHediff(hediff);
            }
        }
    }
}