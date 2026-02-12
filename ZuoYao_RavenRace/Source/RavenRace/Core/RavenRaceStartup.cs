using System.Reflection;
using HarmonyLib;
using Verse;
using System;
using System.Collections.Generic; // 引入字典所需的命名空间

namespace RavenRace
{
    [StaticConstructorOnStartup]
    public static class RavenRaceStartup
    {
        // 创建一个静态只读字典来存储Mod兼容性日志信息
        // Key: Mod的PackageId, Value: 要在日志中打印的趣味消息
        private static readonly Dictionary<string, string> CompatLogMessages = new Dictionary<string, string>
        {
            { "void.charactereditor",         "你知道的，我们支持CE..." },
            { "matathias.tdiner",             "正在让大家上桌吃饭..." },
            { "Ancot.MiliraRace",             "正在摸米莉拉的翅膀根..." },
            { "Ariandel.MiliraImperium",      "正在将灵卵塞入艾蕾德尔的后门..." },
            { "HenTaiLoliTeam.Axolotl",       "正在嗦可爱萌螈的小脚..." },
            { "fxz.moelotlzombie.update",     "正在玩弄满道长的墨镜..." },
            { "Draconis.Koelime",             "正在揉饺龙的肚子..." },
            { "MelonDove.WolfeinRace",        "正在吸沃芬的毛绒绒大尾巴..." },
            { "HAR.MuGirlRace",               "正在喝雪牛娘的奶..." },
            { "RooAndGloomy.DragonianRaceMod","正在把玩龙娘的珠子..." },
            { "Nemonian.MY2.Beta",            "正在用巨大触手攻击茉约的子宫..." },
            { "Epona.EponaDynasticRise",      "正在和艾波娜一起玩赛马娘..." },
            { "LepechandEusro.Tailin",        "正在拍小泰临的屁股..." },
            { "BreadMo.Cinders",              "正在感受余烬的温暖..." },
        };

        static RavenRaceStartup()
        {
            // 使用常量 ID
            var harmony = new HarmonyLib.Harmony(RavenModConstants.HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // 诱饵缓存清理 (防止重载存档时残留)
            Features.DefenseSystem.RavenDecoyCache.Clear();

            Log.Message($"[{RavenModConstants.PackageId}] 左爻小姐正在无聊的自我安慰...");

            // 特殊处理RJW，因为它需要调用一个方法，而不仅仅是打印日志
            if (ModsConfig.IsActive("rim.job.world"))
            {
                if (ModsConfig.IsActive("rjw.menstruation"))
                {
                    Log.Message($"[{RavenModConstants.PackageId}] 饿啊，我们是高贵的三字母玩家！");
                    ApplyRJWCompat();
                }
            }

            // 遍历字典，检查所有其他兼容Mod并打印日志
            foreach (var entry in CompatLogMessages)
            {
                if (ModsConfig.IsActive(entry.Key))
                {
                    Log.Message($"[{RavenModConstants.PackageId}] {entry.Value}");
                }
            }
        }

        private static void ApplyRJWCompat()
        {
            try
            {
                var compatInitializer = GenTypes.GetTypeInAnyAssembly("RavenRace.RJWCompat.RJWCompat_Startup");
                if (compatInitializer != null)
                {
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