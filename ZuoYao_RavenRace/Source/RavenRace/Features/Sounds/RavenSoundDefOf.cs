using RimWorld;
using Verse;

namespace RavenRace.Features.Sounds
{
    [DefOf]
    public static class RavenSoundDefOf
    {
        // 对应 SoundDefs_Meme.xml 中的 defName
        public static SoundDef RavenMeme_TakeDamage;
        public static SoundDef RavenMeme_ArchonTreasure;
        public static SoundDef RavenMeme_BinahAbility;
        public static SoundDef RavenMeme_PawnDowned;
        public static SoundDef RavenMeme_WatchAV;
        public static SoundDef RavenMeme_SocialFail;
        public static SoundDef RavenMeme_CraftFail;
        public static SoundDef RavenMeme_Insulted;
        public static SoundDef RavenMeme_PawnDeath;
        public static SoundDef RavenMeme_Fleeing;

        static RavenSoundDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RavenSoundDefOf));
        }
    }
}