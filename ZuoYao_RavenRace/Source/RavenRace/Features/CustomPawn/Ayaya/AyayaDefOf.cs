using RimWorld;
using Verse;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    /// <summary>
    /// Ayaya 专属 Def 引用集合
    /// 用于建立 XML 定义与 C# 逻辑之间的静态链接
    /// </summary>
    [DefOf]
    public static class AyayaDefOf
    {
        // PawnKind (角色类型)
        public static PawnKindDef Raven_PawnKind_Ayaya;

        // Abilities (技能定义)
        public static AbilityDef Raven_Ability_Ayaya_MusouFuujin;
        public static AbilityDef Raven_Ability_Ayaya_TenguOtoshi;

        // ThingDefs (飞行器与投射物)
        public static ThingDef Raven_PawnFlyer_AyayaDash;
        public static ThingDef Raven_PawnFlyer_Knockback;
        public static ThingDef Raven_Projectile_WindBlade;

        // SoundDefs (音效定义)
        // [修复] 必须在这里声明字段，RimWorld 启动时会自动将 XML 中的同名 Def 绑定到此处
        public static SoundDef Raven_Sound_Ayaya_Slash;
        public static SoundDef Raven_Sound_WindDash;

        // Effects (特效定义)
        public static EffecterDef Raven_Effecter_TenguGale;

        /// <summary>
        /// 静态构造函数，确保 DefOf 在游戏加载时正确初始化
        /// </summary>
        static AyayaDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AyayaDefOf));
        }
    }
}