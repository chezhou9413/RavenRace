using RimWorld;
using Verse;

namespace RavenRace
{
    [DefOf]
    public static class FusangDefOf
    {
        // 派系
        public static FactionDef Fusang_Hidden;

        // 建筑
        public static ThingDef Raven_FusangRadio;

        // 工作/Job
        public static JobDef Raven_Job_UseFusangRadio;

        // 交易类型 (你需要在XML中定义这个，或者暂时用 Base_Outlander_Standard)
        // public static TraderKindDef Fusang_TraderKind; 

        static FusangDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FusangDefOf));
        }
    }
}