using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Features.StoryEngine
{
    /// <summary>
    /// 定义一段完整的对话流程。
    /// 替代原本的 DialogueDef。
    /// </summary>
    public class StoryDef : Def
    {
        // 触发此对话的条件列表
        public List<StoryCondition> conditions = new List<StoryCondition>();

        // 优先级（当多个对话满足条件时，选权重高的）
        public float priority = 1f;

        // 起始节点 ID
        public string initialNodeID = "Start";

        // [新增] 如果为 true，则忽略 initialNodeID，从 nodes 中随机选择一个作为起始节点
        // 用于实现“随机闲聊”功能
        public bool randomStart = false;

        // 所有对话节点
        public List<StoryNode> nodes;

        // 如果这是一段一次性的剧情，完成后设置此 Flag
        public string completeFlag;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors()) yield return error;
            if (nodes.NullOrEmpty()) yield return "Nodes list is empty.";
        }
    }

    /// <summary>
    /// 对话中的一个节点（一句台词）。
    /// </summary>
    public class StoryNode
    {
        public string id; // 唯一标识符，如 "Start", "Ask_About_Fusang"

        [MustTranslate]
        public string text; // 台词文本

        public string speakerName; // 说话人名字（可选，默认左爻）
        public string expressionKey; // 表情 Key，如 "smile", "angry"
        public string backgroundPath; // 背景图路径（可选）
        public string portraitPath; // 强制指定立绘路径（可选）

        // 如果为 true，点击任何选项或播放完直接关闭窗口
        public bool closeDialogue = false;

        // 玩家的选项列表
        public List<StoryOption> options = new List<StoryOption>();

        // 进入此节点时立即执行的动作
        public List<StoryAction> onEnterActions = new List<StoryAction>();
    }

    /// <summary>
    /// 玩家的一个选择项。
    /// </summary>
    public class StoryOption
    {
        [MustTranslate]
        public string text; // 选项文本

        public string nextNodeID; // 跳转到的节点 ID

        [MustTranslate]
        public string tooltip; // 鼠标悬停提示

        // 点击此选项是否直接关闭对话
        public bool closeDialogue = false;

        // 选项显示的条件（如果不满足则不显示）
        public List<StoryCondition> conditions = new List<StoryCondition>();

        // 点击后执行的动作（如加好感、给物品）
        public List<StoryAction> actions = new List<StoryAction>();
    }
}