using RimWorld;
using Verse;

namespace RavenRace.Features.UniqueWeapons.SpiritBeads
{
    [DefOf]
    public static class SpiritBeadsDefOf
    {
        // Jobs
        public static JobDef Raven_Job_InsertBeads;

        // Hediffs
        public static HediffDef Raven_Hediff_SpiritBeadsInserted; // 纳刀 Buff
        public static HediffDef Raven_Hediff_HighClimax;          // 高潮 Debuff

        static SpiritBeadsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SpiritBeadsDefOf));
        }
    }
}