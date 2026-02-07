using Verse;
using RimWorld;
using HarmonyLib; // 需要引用 Harmony

namespace RavenRace.Compat.Milira
{
    [StaticConstructorOnStartup]
    public static class MiliraCompatUtility
    {
        public static bool IsMiliraActive { get; private set; }
        public static bool IsPathingPatchActive { get; private set; }

        static MiliraCompatUtility()
        {
            // 1. 检测米莉拉主模组
            IsMiliraActive = DefDatabase<ThingDef>.GetNamedSilentFail("Milira_Race") != null
                             || DefDatabase<ThingDef>.GetNamedSilentFail("Milira") != null;

            // 2. 检测天羽族真实飞行补丁
            // 优先检查 ModID
            IsPathingPatchActive = ModsConfig.IsActive("Milira.Independent.Pathing");

            // 如果 ModID 没找到，尝试反射查找其核心类 (软兼容)
            if (!IsPathingPatchActive)
            {
                // 飞行补丁通常包含这个类
                if (AccessTools.TypeByName("Milira.Pathing.MiliraPathingGlobal") != null)
                {
                    IsPathingPatchActive = true;
                }
            }
        }
    }
}