using RimWorld;
using Verse;

namespace RavenRace.Features.CustomPawn.Binah
{
    [DefOf]
    public static class BinahDefOf
    {
        // PawnKind
        [MayRequire("Chezhou.ChezhouLib.lib")]
        public static PawnKindDef Raven_PawnKind_Binah;

        // Abilities
        public static AbilityDef Raven_Ability_Binah_PillarShot;
        public static AbilityDef Raven_Ability_Binah_Shockwave;
        public static AbilityDef Raven_Ability_Binah_DegradationPillar;
        public static AbilityDef Raven_Ability_Binah_DegradationLock;

        // Hediffs
        public static HediffDef Raven_Hediff_Binah_DegradationLock;

        // Projectiles
        public static ThingDef Raven_Projectile_Binah_Pillar_I;
        public static ThingDef Raven_Projectile_Binah_Pillar_II;
        public static ThingDef Raven_Projectile_Binah_Pillar_III;
        public static ThingDef Raven_Projectile_Binah_Pillar_IV;
        public static ThingDef Raven_Projectile_Binah_Degradation;

        // Motes (Pillar Visuals)
        public static ThingDef Raven_Mote_Binah_Pillar_I;
        public static ThingDef Raven_Mote_Binah_Pillar_II;
        public static ThingDef Raven_Mote_Binah_Pillar_III;
        public static ThingDef Raven_Mote_Binah_Pillar_IV;

        // Motes (Effects)
        public static ThingDef Raven_Mote_Binah_FairyTrail;

        // [修复] 启用震击波 Mote 定义
        public static ThingDef Raven_Mote_Binah_ShockwaveDistortion;

        static BinahDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BinahDefOf));
        }
    }
}