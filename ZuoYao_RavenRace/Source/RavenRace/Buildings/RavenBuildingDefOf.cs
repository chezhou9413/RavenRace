using RimWorld;
using Verse;

namespace RavenRace.Buildings
{
    [DefOf]
    public static class RavenBuildingDefOf
    {
        // 对应 Defs/Buildings/Thoughts/Thoughts_Buildings.xml 中的 defName
        public static ThoughtDef Raven_Thought_IncenseSmell;

        static RavenBuildingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RavenBuildingDefOf));
        }
    }
}