using System;
using System.Linq;
using Verse;

namespace RavenRace.Features.UpdateNews
{
    /// <summary>
    /// 伴随游戏生命周期的静默检测器。
    /// 负责在读取存档或开始新游戏时，检查是否需要弹出更新日志 UI。
    /// </summary>
    public class GameComponent_UpdateNews : GameComponent
    {
        public GameComponent_UpdateNews(Game game)
        {
            // 必须有此构造函数供引擎实例化
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            CheckAndPopNews();
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            CheckAndPopNews();
        }

        /// <summary>
        /// 核心弹窗检测逻辑。
        /// 已修改为：每次进档必然弹出，除非玩家主动勾选了“不再自动弹出”。
        /// </summary>
        private void CheckAndPopNews()
        {
            // 使用 LongEventHandler，确保在游戏完全加载完毕、回到主渲染线程后再执行！
            // 这彻底解决了 "Tried to get a resource from a different thread" 的跨线程加载贴图红字崩溃问题
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                // 1. 检查总开关：如果玩家在UI底部的复选框选择了“不再自动弹出”，则静默跳过
                if (!RavenRaceMod.Settings.enableUpdateNews) return;

                // 2. 安全检查：确保有可以读取的更新日志数据
                var allDefs = DefDatabase<RavenUpdateNewsDef>.AllDefs.ToList();
                if (allDefs.Count == 0) return;

                // 3. 直接弹出炫酷界面 (移除了复杂的版本号比对逻辑，增强视觉提醒)
                Find.WindowStack.Add(new Window_UpdateNews());
            });
        }
    }
}