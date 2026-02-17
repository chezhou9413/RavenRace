using RimWorld;
using Verse;

namespace RavenRace.Features.CustomPawn.ZuoYao
{
    [DefOf]
    public static class ZuoYaoDefOf
    {
        // PawnKind
        public static PawnKindDef Raven_PawnKind_ZuoYao;

        // Abilities
        public static AbilityDef Raven_Ability_Kotoamatsukami;

        // Relations
        public static PawnRelationDef Raven_Relation_AbsoluteMaster;
        public static PawnRelationDef Raven_Relation_LoyalServant;

        // Thoughts
        public static ThoughtDef Raven_Thought_Kotoamatsukami_Recruited;

        static ZuoYaoDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ZuoYaoDefOf));
        }
    }
}