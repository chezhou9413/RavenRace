using RimWorld;
using Verse;

namespace RavenRace.Features.DegradationCharm
{
    [DefOf]
    public static class DegradationCharmDefOf
    {
        // 物品
        public static ThingDef Raven_Item_CorruptionTalisman;
        // Hediff
        public static HediffDef Raven_Hediff_Degradation;
        // 特性
        public static TraitDef Raven_Trait_Lecherous;
        // 工作
        public static JobDef Raven_Job_ApplyCharm;
        public static JobDef Raven_Job_RemoveCharm;

        static DegradationCharmDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DegradationCharmDefOf));
        }
    }
}