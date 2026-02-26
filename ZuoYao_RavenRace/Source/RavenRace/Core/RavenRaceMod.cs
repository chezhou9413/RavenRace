using UnityEngine;
using Verse;

namespace RavenRace
{
    /// <summary>
    /// 模组的主入口类，继承自Verse.Mod。RimWorld加载Mod时会自动实例化这个类。
    /// 主要职责是：
    /// 1. 加载和管理模组的设置 (RavenRaceSettings)。
    /// 2. 提供设置界面的入口和绘制逻辑。
    /// 3. 持有对模组内容包 (ModContentPack) 的引用。
    /// </summary>
    public class RavenRaceMod : Mod
    {
        /// <summary>
        /// 全局静态属性，用于在整个模组中方便地访问设置实例。
        /// </summary>
        public static RavenRaceSettings Settings { get; private set; }

        /// <summary>
        /// 全局静态属性，提供对当前Mod内容包的访问，可用于加载资源等。
        /// </summary>
        public static ModContentPack ContentPack { get; private set; }

        /// <summary>
        /// 模组的构造函数，在游戏启动时由RimWorld的Mod加载系统自动调用。
        /// </summary>
        /// <param name="content">RimWorld传递过来的模组内容包实例。</param>
        public RavenRaceMod(ModContentPack content) : base(content)
        {
            ContentPack = content;
            // 获取或创建模组设置的实例。如果存档中有，则加载；否则创建新的。
            Settings = GetSettings<RavenRaceSettings>();

            // 注意：Harmony补丁的加载已移至 RavenRaceStartup.cs。
            // 这样做是为了利用 [StaticConstructorOnStartup] 特性，确保所有Harmony补丁在所有Defs（XML定义）加载完毕后才应用，
            // 从而避免因补丁执行时Def尚未加载而引发的“红字”错误，并能更好地处理Mod间的加载顺序和兼容性问题。

            Log.Message($"[{RavenModConstants.PackageId}] 渡鸦模组已成功启动！");
        }

        /// <summary>
        /// 返回在游戏选项 -> Mod设置中显示的此Mod的名称。
        /// </summary>
        public override string SettingsCategory()
        {
            // 使用翻译键以支持多语言。
            return "RavenRace_SettingsCategory".Translate();
        }

        /// <summary>
        /// 绘制Mod设置窗口的UI内容。
        /// 此方法将具体的绘制逻辑委托给了 Settings 对象本身。
        /// </summary>
        /// <param name="inRect">可供绘制的矩形区域。</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// 当玩家在设置菜单中修改了设置后，RimWorld会调用此方法来保存它们。
        /// 我们在这里也调用一个自定义的回调，以应用某些需要即时生效的更改。
        /// </summary>
        public override void WriteSettings()
        {
            base.WriteSettings();
            Settings.OnSettingsChanged();
        }
    }
}