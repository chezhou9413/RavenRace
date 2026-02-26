namespace RavenRace
{
    /// <summary>
    /// 存储模组全局常量的静态类。
    /// 使用常量可以避免在代码中硬编码“魔法字符串”，便于统一管理和未来修改，并减少拼写错误。
    /// </summary>
    public static class RavenModConstants
    {
        // ================== 核心标识符 ==================

        /// <summary>
        /// Mod的包ID，必须与 About/About.xml 中的 packageId 完全一致。
        /// </summary>
        public const string PackageId = "ZuoYao.RavenRace";

        /// <summary>
        /// Harmony补丁的唯一ID，用于避免与其他Mod的补丁冲突。
        /// </summary>
        public const string HarmonyId = "ZuoYao.RavenRace.Harmony";


        // ================== 核心DefName ==================

        /// <summary>
        /// 渡鸦种种族的DefName。
        /// </summary>
        public const string RavenRaceDefName = "Raven_Race";

        /// <summary>
        /// 扶桑隐藏派系的DefName。
        /// </summary>
        public const string FusangFactionDefName = "Fusang_Hidden";


        // ================== 第三方Mod包ID ==================

        /// <summary>
        /// 面部动画Mod的官方包ID。
        /// </summary>
        public const string FacialAnimationModId = "Nals.FacialAnimation";

        /// <summary>
        /// 面部动画Mod在创意工坊的WIP（开发中）版本的包ID。
        /// </summary>
        public const string FacialAnimationWIPModId = "2850854272";
    }
}