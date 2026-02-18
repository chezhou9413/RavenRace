using Verse;
using RavenRace.Features.Operator; // 引用 OperatorManager

namespace RavenRace.Features.StoryEngine
{
    /// <summary>
    /// 剧情条件的抽象基类。
    /// </summary>
    public abstract class StoryCondition
    {
        // 核心检查方法
        public abstract bool IsMet();
    }

    // ================= 具体实现 =================

    /// <summary>
    /// 检查全局 Flag 是否存在（用于判断是否初见、是否完成过某任务）。
    /// </summary>
    public class StoryCondition_HasFlag : StoryCondition
    {
        public string flagKey;
        public bool invert = false; // 如果为 true，则表示“必须没有此 Flag”

        public override bool IsMet()
        {
            bool has = StoryWorldComponent.HasFlag(flagKey);
            return invert ? !has : has;
        }
    }

    /// <summary>
    /// 检查左爻的好感度范围。
    /// </summary>
    public class StoryCondition_Favorability : StoryCondition
    {
        public int min = -9999;
        public int max = 9999;

        public override bool IsMet()
        {
            var manager = Find.World.GetComponent<WorldComponent_OperatorManager>();
            if (manager == null) return false;
            return manager.zuoYaoFavorability >= min && manager.zuoYaoFavorability <= max;
        }
    }

    /// <summary>
    /// 检查是否拥有足够的资源（如情报点）。
    /// </summary>
    public class StoryCondition_HasResource : StoryCondition
    {
        public FusangResourceType type;
        public int amount;

        public override bool IsMet()
        {
            return FusangResourceManager.GetAmount(type) >= amount;
        }
    }
}