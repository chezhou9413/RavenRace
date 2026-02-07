namespace RavenRace
{
    /// <summary>
    /// 模组全局常量定义
    /// 用于消除代码中的魔法字符串 (Magic Strings)
    /// </summary>
    public static class RavenModConstants
    {
        // Mod Package ID (必须与 About.xml 一致)
        public const string PackageId = "ZuoYao.RavenRace";

        // Harmony Patch ID
        public const string HarmonyId = "ZuoYao.RavenRace.Harmony";

        // 关键 DefNames (用于 DefOf 映射前的临时或特殊用途)
        public const string RavenRaceDefName = "Raven_Race";
        public const string FacialAnimationModId = "Nals.FacialAnimation";
        public const string FacialAnimationWIPModId = "2850854272";
    }
}