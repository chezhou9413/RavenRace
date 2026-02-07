using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RavenRace
{
    /// <summary>
    /// 模组的主入口类。
    /// 负责加载设置和初始化 Mod 对象。
    /// </summary>
    public class RavenRaceMod : Mod
    {
        /// <summary>
        /// 全局 Mod 设置实例
        /// </summary>
        public static RavenRaceSettings Settings { get; private set; }

        /// <summary>
        /// 模组内容包引用
        /// </summary>
        public static ModContentPack ContentPack { get; private set; }

        /// <summary>
        /// 构造函数，由游戏自动调用
        /// </summary>
        public RavenRaceMod(ModContentPack content) : base(content)
        {
            ContentPack = content;
            Settings = GetSettings<RavenRaceSettings>();

            // 注意：Harmony 补丁的加载已移交至 RavenRaceStartup.cs
            // 这样做是为了确保 Defs 加载完毕后再应用某些依赖 Def 的补丁，
            // 并且可以更好地处理 Mod 冲突。

            Log.Message($"[{RavenModConstants.PackageId}] Mod initialized. Version 1.0.0");
        }

        /// <summary>
        /// 设置菜单的标题
        /// </summary>
        public override string SettingsCategory()
        {
            return "RavenRace_SettingsCategory".Translate();
        }

        /// <summary>
        /// 绘制设置菜单的内容
        /// </summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// 当设置被修改并保存时调用
        /// </summary>
        public override void WriteSettings()
        {
            base.WriteSettings();
            Settings.OnSettingsChanged();
        }
    }
}