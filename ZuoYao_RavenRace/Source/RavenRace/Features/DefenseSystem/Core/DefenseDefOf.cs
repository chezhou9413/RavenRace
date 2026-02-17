using RimWorld;
using Verse;

namespace RavenRace
{
    [DefOf]
    public static class DefenseDefOf
    {
        // 研究项目
        public static ResearchProjectDef RavenResearch_TrapBasics;
        public static ResearchProjectDef RavenResearch_DecoyTech;
        public static ResearchProjectDef RavenResearch_ChemicalWarfare;

        // Hediffs
        public static HediffDef RavenHediff_TendonCut;
        public static HediffDef RavenHediff_AnestheticBuildup;
        public static HediffDef RavenHediff_AphrodisiacEffect;
        public static HediffDef RavenHediff_LegoPain;
        public static HediffDef RavenHediff_ConceptionProcess;

        // ThingDefs
        public static ThingDef RavenDecoy_Dummy;
        public static ThingDef Raven_TrapWall;
        public static ThingDef RavenGas_Anesthetic;
        public static ThingDef RavenGas_Aphrodisiac;
        public static ThingDef RavenTrap_Lego;
        public static ThingDef RavenTrap_Conception;

        public static ThingDef RavenTrap_RisingWall;

        public static ThingDef RavenGas_BlackMist; 

        public static ThingDef RavenTrap_Beast;

        // 伪装系统
        public static JobDef Raven_Job_EnterConcealment;




        static DefenseDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefenseDefOf));
        }
    }
}