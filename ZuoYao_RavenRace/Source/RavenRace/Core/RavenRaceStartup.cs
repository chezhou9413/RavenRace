using System.Reflection;
using HarmonyLib;
using Verse;
using System;
using System.Collections.Generic;

namespace RavenRace
{
    /// <summary>
    /// 模组启动器，利用 [StaticConstructorOnStartup] 特性确保在游戏主菜单加载时执行。
    /// 这是应用Harmony补丁和执行一次性初始化逻辑的最佳位置。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class RavenRaceStartup
    {
        // 存储兼容Mod及其趣味加载信息的字典。
        private static readonly Dictionary<string, string> CompatLogMessages = new Dictionary<string, string>
        {
            { "void.charactereditor",         "你知道的，我们支持CE..." },
            { "matathias.tdiner",             "正在让大家上桌吃饭..." },
            { "Ancot.MiliraRace",             "正在摸米莉拉的翅膀根..." },
            { "Ariandel.MiliraImperium",      "正在将灵卵塞入艾蕾德尔的后门..." },
            { "hentailoliteam.axolotl",       "正在嗦可爱萌螈的小脚..." },
            { "fxz.moelotlzombie.update",     "正在玩弄满道长的墨镜..." },
            { "Draconis.Koelime",             "正在揉饺龙的肚子..." },
            { "MelonDove.WolfeinRace",        "正在吸沃芬的毛绒绒大尾巴..." },
            { "HAR.MuGirlRace",               "正在喝雪牛娘的奶..." },
            { "RooAndGloomy.DragonianRaceMod","正在把玩龙娘的珠子..." },
            { "Nemonian.MY2.Beta",            "正在用巨大触手攻击茉约的子宫..." },
            { "Epona.EponaDynasticRise",      "正在和艾波娜一起玩赛马娘..." },
            { "LepechandEusro.Tailin",        "正在拍小泰临的屁股..." },
            { "BreadMo.Cinders",              "正在感受余烬的温暖..." },
            { "Tourswen.LegendaryBlackDragon",  "正在请黑龙给灶台生火..." },
            { "SutSutMan.MinchoTheMintChocoSlimeHARver",  "正在品尝美味的珉巧...嗯，是小人，不是冰淇淋！" },
            { "Aurora.Nebula.NemesisRaceThePunisher", "正在赐予纳美西斯甘甜的苦痛..." },
            { "Golden.GloriasMod", "正在与煌金族共享金色的荣耀..." },
            { "keeptpa.NivarianRace", "正在感受涅瓦莲冰冷坚毅的不屈之心..." },
        };

        /// <summary>
        /// 静态构造函数，在游戏启动时自动执行。
        /// </summary>
        static RavenRaceStartup()
        {
            // 创建Harmony实例并应用当前程序集中的所有补丁。
            var harmony = new HarmonyLib.Harmony(RavenModConstants.HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // 诱饵缓存清理 (防止重载存档时残留旧数据)
            Features.DefenseSystem.RavenDecoyCache.Clear();

            Log.Message($"[{RavenModConstants.PackageId}] 左爻小姐正在无聊的自我安慰...");

            // 特殊处理RJW，因为它需要通过反射调用特定的方法来应用补丁。
            if (ModsConfig.IsActive("rim.job.world"))
            {
                if (ModsConfig.IsActive("rjw.menstruation"))
                {
                    Log.Message($"[{RavenModConstants.PackageId}] 饿啊，我们是高贵的三字母玩家！");
                    ApplyRJWCompat();
                }
            }

            // 遍历兼容字典，检查已激活的Mod并打印趣味日志。
            foreach (var entry in CompatLogMessages)
            {
                if (ModsConfig.IsActive(entry.Key))
                {
                    Log.Message($"[{RavenModConstants.PackageId}] {entry.Value}");
                }
            }
        }

        /// <summary>
        /// 通过反射安全地应用RJW兼容性补丁。
        /// 这样做可以避免在未安装RJW时因找不到相关类而导致游戏崩溃。
        /// </summary>
        private static void ApplyRJWCompat()
        {
            try
            {
                // 通过类名字符串查找RJW兼容模块的启动类。
                var compatInitializer = GenTypes.GetTypeInAnyAssembly("RavenRace.RJWCompat.RJWCompat_Startup");
                if (compatInitializer != null)
                {
                    // 查找并调用其静态的 ApplyPatches 方法。
                    var applyMethod = compatInitializer.GetMethod("ApplyPatches", BindingFlags.Public | BindingFlags.Static);
                    if (applyMethod != null)
                    {
                        applyMethod.Invoke(null, null);
                    }
                    else
                    {
                        Log.Error($"[{RavenModConstants.PackageId}] Could not find 'ApplyPatches' method in RJWCompat_Startup.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[{RavenModConstants.PackageId}] Exception while applying RJW compatibility patches: {ex}");
            }
        }
    }
}