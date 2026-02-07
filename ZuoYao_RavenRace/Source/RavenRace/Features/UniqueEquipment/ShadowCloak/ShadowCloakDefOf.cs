using RimWorld;
using Verse;

namespace RavenRace.Features.UniqueEquipment.ShadowCloak
{
    [DefOf]
    public static class ShadowCloakDefOf
    {
        public static ThingDef Raven_Apparel_ShadowCloak;
        public static HediffDef Raven_Hediff_ShadowStep; // 确保这个在你原有的XML里定义了，或者通过查找得到
        public static HediffDef Raven_Hediff_ShadowAttackCooldown; // 新增

        static ShadowCloakDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ShadowCloakDefOf));
        }
    }
}