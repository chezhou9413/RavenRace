using RimWorld;
using Verse;

namespace RavenRace.Features.CustomPawn.Binah
{
    [DefOf]
    public static class BinahDefOf
    {
        // --- 技能 Abilities ---
        public static AbilityDef Raven_Ability_Binah_PillarShot;
        public static AbilityDef Raven_Ability_Binah_Shockwave;
        public static AbilityDef Raven_Ability_Binah_DegradationPillar;
        public static AbilityDef Raven_Ability_Binah_DegradationLock;

        // --- 状态 Hediffs ---
        public static HediffDef Raven_Hediff_Binah_DegradationLock;

        // --- 投射物与特效 Things ---
        public static ThingDef Raven_Projectile_Binah_Pillar_I;
        public static ThingDef Raven_Projectile_Binah_Pillar_II;
        public static ThingDef Raven_Projectile_Binah_Pillar_III;
        public static ThingDef Raven_Projectile_Binah_Pillar_IV;

        // [Fix] 添加缺失的劣化之柱投射物
        public static ThingDef Raven_Projectile_Binah_Degradation;

        public static ThingDef Raven_Mote_Binah_FairyTrail;
        public static ThingDef Raven_Mote_Binah_ShockwaveDistortion;

        static BinahDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BinahDefOf));
        }
    }
}