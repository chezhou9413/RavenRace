using HarmonyLib;
using System.Collections.Generic; // [修正] 需要引用 Generic 集合
using System.Reflection;
using Verse;

namespace RavenRace.RJWCompat
{
    // 这个类不再需要 [StaticConstructorOnStartup]，因为它是由主模组动态调用的
    public static class RJWCompat_Startup
    {
        /// <summary>
        /// 这个公共静态方法是整个兼容模块的入口点。
        /// 它由主模组的启动器在确认RJW已加载后，通过反射来调用。
        /// </summary>
        public static void ApplyPatches()
        {
            // 修改为（加上 HarmonyLib. 前缀）：
            var harmony = new HarmonyLib.Harmony("ZuoYao.RavenRace.RJWCompat");

            Log.Message("[RavenRace RJWCompat] ApplyPatches() called by main module. Applying patches now...");

            // [最终修复] 手动应用整个程序集的补丁，这是最标准和可靠的方式。
            // Harmony 会自动扫描这个DLL里的所有 [HarmonyPatch] 标签并应用它们。
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("[RavenRace RJWCompat] All patches from RJWCompat assembly have been applied.");

            // 在所有补丁都应用完毕后，安全地初始化反射缓存。
            // 这个时候去查找RJW的类型是100%安全的。
            RjwReflection.Initialize();
        }
    }
}